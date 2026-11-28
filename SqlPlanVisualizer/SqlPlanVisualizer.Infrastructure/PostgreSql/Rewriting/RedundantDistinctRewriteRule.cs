using PgSqlParser;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class RedundantDistinctRewriteRule(IEnumerable<DatabaseTableMetadata>? tables) : RelationalRewriteRule(tables)
{
    protected override string RuleId => "REDUNDANT_DISTINCT_REWRITE";
    protected override string Title => "A kulcs már biztosítja az egyediséget";
    protected override string Explanation => "A kiválasztott oszlopok tartalmazzák a tábla teljes, nem halasztható elsődleges kulcsát. Ebben az egyszerű lekérdezésben a DISTINCT elhagyható.";
    protected override bool Supports(SelectStmt s) => RewriteAst.Supported(s) && s.GroupClause.Count == 0 &&
        s.HavingClause is null && s.WindowClause.Count == 0 && s.ValuesLists.Count == 0;

    private static bool HasUniqueProjection(SelectStmt s, RangeVar relation, DatabaseTableMetadata table)
    {
        var projection = s.Clone();
        projection.TargetList.Clear();
        foreach (var target in s.TargetList)
        {
            var fields = target.ResTarget?.Val?.ColumnRef?.Fields;
            bool star = fields is not null && (fields.Count == 1 && fields[0].AStar is not null ||
                fields.Count == 2 && fields[0].String?.Sval == relation.Alias?.Aliasname && fields[1].AStar is not null);
            if (!star) { projection.TargetList.Add(target.Clone()); continue; }
            foreach (var column in table.Columns)
            {
                var parsed = RewriteAst.Parse($"SELECT {Quote(relation.Alias!.Aliasname)}.{Quote(column.Name)}");
                projection.TargetList.Add(parsed.Stmts[0].Stmt.SelectStmt.TargetList[0]);
            }
        }
        return UniqueProjection(projection, relation, table);
    }

    private static string Quote(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";

    protected override bool Rewrite(SelectStmt s)
    {
        if (!PlainDistinct(s) || s.FromClause[0].RangeVar is not { } relation || Table(relation) is not { } table || !HasUniqueProjection(s, relation, table)) return false;
        s.DistinctClause.Clear(); return true;
    }
}
