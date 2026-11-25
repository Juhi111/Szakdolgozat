using PgSqlParser;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>Kulcs- és NULL-feltételekre épülő átírások közös, szándékosan szűk bizonyítási kerete.</summary>
public abstract class RelationalRewriteRule(IEnumerable<DatabaseTableMetadata>? tables) : ISqlRewriteRule
{
    private readonly IReadOnlyList<DatabaseTableMetadata> _tables = tables?.ToArray() ?? Array.Empty<DatabaseTableMetadata>();
    protected abstract string RuleId { get; }
    protected abstract string Title { get; }
    protected abstract string Explanation { get; }
    protected abstract bool Rewrite(SelectStmt select);
    public static bool IsRelationalRule(string id) => id is "NOT_IN_EXISTS_REWRITE" or "LEFT_JOIN_ANTI_REWRITE" or "REDUNDANT_DISTINCT_REWRITE" or "DISTINCT_JOIN_EXISTS_REWRITE";

    public IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql)
    {
        if (_tables.Count == 0) return Array.Empty<SqlRewriteSuggestion>();
        ParseResult parsed;
        try { parsed = RewriteAst.Parse(sql); }
        catch (FormatException) { return Array.Empty<SqlRewriteSuggestion>(); }
        if (parsed.Stmts.Count != 1 || parsed.Stmts[0].Stmt.SelectStmt is not { } select || !Simple(select) || !Rewrite(select))
            return Array.Empty<SqlRewriteSuggestion>();
        var output = Parser.Deparse(parsed);
        if (output.Error is not null || string.IsNullOrWhiteSpace(output.Value)) return Array.Empty<SqlRewriteSuggestion>();
        try { RewriteAst.Parse(output.Value); }
        catch (FormatException) { return Array.Empty<SqlRewriteSuggestion>(); }
        return new[] { new SqlRewriteSuggestion
        {
            RuleId = RuleId, Title = Title, OriginalSql = sql, SuggestedSql = output.Value, Explanation = Explanation,
            RequiresRelationalMetadata = true,
            Preconditions = "A javaslat az azonosított táblák aktuális kulcs-, típus- és NULL-adataira épül. Futás előtt ezeket újra ellenőrizzük. " +
                "Csak sémával megadott, közvetlen táblákat és egyszerű oszlophivatkozásokat támogatunk; öröklődést, partíciókat és sorbiztonsági szabályokat nem. " +
                "Az eredmények sorrendje ORDER BY nélkül nem garantált; a gyorsulást külön méréssel ellenőrizd."
        } };
    }

    public static IReadOnlyList<DatabaseTableReference> TableReferences(string sql)
    {
        var parsed = RewriteAst.Parse(sql);
        var references = new HashSet<DatabaseTableReference>();
        // Explicit schemas cannot refer to CTE names, and do not depend on search_path resolution.
        new PostgreSqlAstWalker().Walk(parsed, 1, entry =>
        {
            if (entry.Node is RangeVar relation && !string.IsNullOrEmpty(relation.Schemaname) && string.IsNullOrEmpty(relation.Catalogname))
                references.Add(new(relation.Schemaname, relation.Relname));
        });
        return references.ToArray();
    }

    protected static bool Simple(SelectStmt s) => s.Op == SetOperation.SetopNone && s.WithClause is null && s.IntoClause is null &&
        s.LockingClause.Count == 0 && s.GroupClause.Count == 0 && s.HavingClause is null && s.WindowClause.Count == 0 &&
        s.SortClause.Count == 0 && s.LimitCount is null && s.LimitOffset is null && s.ValuesLists.Count == 0 && s.FromClause.Count == 1;
    protected static bool PlainDistinct(SelectStmt s) => s.DistinctClause.Count == 1 && s.DistinctClause[0].NodeCase == Node.NodeOneofCase.None;

    protected DatabaseTableMetadata? Table(RangeVar? relation)
    {
        if (relation is null || string.IsNullOrEmpty(relation.Schemaname) || !string.IsNullOrEmpty(relation.Catalogname) ||
            relation.Alias is null || string.IsNullOrEmpty(relation.Alias.Aliasname) || relation.Alias.Colnames.Count > 0) return null;
        var matches = _tables.Where(t => t.Schema == relation.Schemaname && t.Table == relation.Relname).ToArray();
        return matches.Length == 1 && matches[0] is { RelationKind: "r", HasDescendants: false, RowSecurity: false } ? matches[0] : null;
    }

    protected static ColumnAnalysisMetadata? Column(Node? node, RangeVar relation, DatabaseTableMetadata table)
    {
        if (node?.ColumnRef is not { } column || column.Fields.Count != 2 || column.Fields.Any(f => f.String is null) ||
            column.Fields[0].String.Sval != relation.Alias?.Aliasname) return null;
        return table.Columns.SingleOrDefault(c => c.Name == column.Fields[1].String.Sval && c.Generated is "" or "s");
    }
    protected static bool KeyType(ColumnAnalysisMetadata? c) => c is { TypeSchema: "pg_catalog", IsDomain: false } && c.TypeName is "int2" or "int4" or "int8" or "uuid";
    protected static bool SameEqualityType(ColumnAnalysisMetadata? a, ColumnAnalysisMetadata? b) => KeyType(a) && KeyType(b) && a!.TypeName == b!.TypeName;
    protected static bool Projection(SelectStmt s, RangeVar relation, DatabaseTableMetadata table) => s.TargetList.Count > 0 && s.TargetList.All(target =>
        target.ResTarget is { } t && t.Indirection.Count == 0 && Column(t.Val, relation, table) is { TypeSchema: "pg_catalog", IsDomain: false } c &&
        c.TypeName is "int2" or "int4" or "int8" or "uuid" or "text" or "varchar" or "bool" or "numeric" or "date" or "timestamp" or "timestamptz");

    protected static bool UniqueProjection(SelectStmt s, RangeVar relation, DatabaseTableMetadata table)
    {
        if (!Projection(s, relation, table)) return false;
        var projected = s.TargetList.Select(n => Column(n.ResTarget.Val, relation, table)!.Name).ToHashSet();
        return table.Keys.Any(key => key.Kind == "p" && key.Validated && !key.Deferrable && key.Columns.Count > 0 &&
            key.Columns.All(name => projected.Contains(name) && table.Columns.Any(c => c.Name == name && KeyType(c) && c.IsNullable == false)) &&
            table.Indexes.Any(i => i.Name == key.IndexName && i.IsPrimary && i.IsUnique && i.IsUsable && i.Method == "btree" &&
                i.HasDefaultOperatorClasses && i.Predicate is null &&
                i.Elements.Where(e => !e.Included).OrderBy(e => e.Position).Select(e => e.Column).SequenceEqual(key.Columns)));
    }

    protected bool Join(SelectStmt s, JoinType kind, out JoinExpr join, out RangeVar left, out RangeVar right,
        out DatabaseTableMetadata leftTable, out DatabaseTableMetadata rightTable)
    {
        join = s.FromClause[0].JoinExpr!; left = null!; right = null!; leftTable = null!; rightTable = null!;
        if (join is null || join.Jointype != kind || join.IsNatural || join.Alias is not null || join.UsingClause.Count != 0 ||
            join.Larg?.RangeVar is not { } l || join.Rarg?.RangeVar is not { } r || Table(l) is not { } lt || Table(r) is not { } rt ||
            l.Alias.Aliasname == r.Alias.Aliasname || join.Quals?.AExpr is not { } predicate || RewriteAst.Op(predicate) != "=") return false;
        var a = Column(predicate.Lexpr, l, lt); var b = Column(predicate.Rexpr, r, rt);
        if (a is null || b is null) { a = Column(predicate.Rexpr, l, lt); b = Column(predicate.Lexpr, r, rt); }
        if (!SameEqualityType(a, b)) return false;
        left = l; right = r; leftTable = lt; rightTable = rt;
        return Projection(s, left, leftTable);
    }

    protected static Node Exists(RangeVar relation, Node predicate, bool negated)
    {
        var query = RewriteAst.Parse("SELECT 1").Stmts[0].Stmt.SelectStmt;
        query.FromClause.Add(new Node { RangeVar = relation.Clone() }); query.WhereClause = predicate.Clone();
        var exists = new Node { SubLink = new SubLink { SubLinkType = SubLinkType.ExistsSublink, Subselect = new Node { SelectStmt = query } } };
        if (!negated) return exists;
        var not = new BoolExpr { Boolop = BoolExprType.NotExpr }; not.Args.Add(exists);
        return new Node { BoolExpr = not };
    }
    protected static void ReplaceJoin(SelectStmt s, RangeVar left, RangeVar right, Node predicate, bool negated)
    {
        s.FromClause.Clear(); s.FromClause.Add(new Node { RangeVar = left.Clone() });
        s.WhereClause = Exists(right, predicate, negated);
    }
}
