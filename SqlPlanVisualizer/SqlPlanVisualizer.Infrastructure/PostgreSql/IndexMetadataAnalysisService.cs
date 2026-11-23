using System.Text.Json;
using PgSqlParser;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

public sealed class IndexMetadataAnalysisService(IDatabaseIndexMetadataService service)
{
    public async Task<IndexMetadataReport> LoadAsync(CombinedAnalysisResult result, DatabaseConnectionOptions options, string password, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(password);
        var origin = result.Pair.Plan.Origin;
        var snapshot = options.Copy();
        if (origin?.Source != PlanSourceKind.Database || origin.Host != snapshot.Host || origin.Port != snapshot.Port ||
            origin.Database != snapshot.Database || origin.Schema != snapshot.Schema || origin.Username != snapshot.Username ||
            origin.StatementIndex != result.Pair.Statement.StatementIndex || string.IsNullOrWhiteSpace(origin.SqlText))
            throw new InvalidOperationException("Ehhez a kapcsolathoz tartozó, adatbázisból lekért terv szükséges. Kérd le újra a tervet.");
        var tables = new List<TableIndexReport>();
        var notices = new List<string>();
        var references = Nodes(result.Pair.Plan.Root).Where(n => n.RelationName is not null)
            .Select(n => new DatabaseTableReference(Text(n, "Schema"), n.RelationName!)).Distinct().ToList();
        if (references.Count > 64) notices.Add("Legfeljebb 64 tábla metaadatait kérjük le.");
        foreach (var reference in references.Take(64))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var table = await service.GetTableAsync(snapshot, password, reference, token);
                if (table is null) notices.Add($"{reference.Schema}.{reference.Table}: a metaadat nem érhető el; ebből nem következik, hogy nincs index.");
                else tables.Add(new(table, Evaluate(table, origin.SqlText!, result.Pair.Plan.Root)));
            }
            catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException)
            {
                token.ThrowIfCancellationRequested();
                notices.Add($"{reference.Schema}.{reference.Table}: a lekérés sikertelen; ellenőrizd a kapcsolatot és a jogosultságokat.");
            }
        }
        token.ThrowIfCancellationRequested();
        if (references.Count == 0) notices.Add("A tervben nem találtunk azonosítható táblát.");
        return new(DateTimeOffset.Now, $"{snapshot.Host}:{snapshot.Port}/{snapshot.Database}; séma: {snapshot.Schema}; felhasználó: {snapshot.Username}", tables, notices);
    }

    public static IReadOnlyList<IndexObservation> Evaluate(DatabaseTableMetadata table, string sql, PlanNode root)
    {
        var observations = new List<IndexObservation>();
        var named = new HashSet<string>();
        void Visit(PlanNode node, bool bitmapParent = false)
        {
            bool matches = node.RelationName == table.Table && Text(node, "Schema") == table.Schema;
            if ((matches || bitmapParent && node.NodeType == "Bitmap Index Scan") && Text(node, "Index Name") is string name) named.Add(name);
            foreach (var child in node.Children) Visit(child, matches && node.NodeType == "Bitmap Heap Scan" || bitmapParent && node.NodeType is "BitmapAnd" or "BitmapOr");
        }
        Visit(root);
        foreach (var index in table.Indexes)
            if (named.Contains(index.Name)) observations.Add(new("A tervben szereplő index", $"{index.Name}: a terv megnevezi ezt az indexet. Ez önmagában nem bizonyítja, hogy a művelet ténylegesen lefutott."));
        if (table.Indexes.Count == 0) observations.Add(new("Indexleltár", "A lekérés időpontjában ezen a reláción nem találtunk indexet. Ez önmagában nem teljesítményprobléma."));
        SelectStmt? select;
        try { var parsed = RewriteAst.Parse(sql); select = parsed.Stmts.Count == 1 ? parsed.Stmts[0].Stmt.SelectStmt : null; }
        catch (FormatException) { select = null; }
        if (!RewriteAst.Supported(select) || RewriteAst.Contains(select!, n => n is SubLink))
        {
            observations.Add(new("Illeszkedésvizsgálat", "Az automatikus illesztés jelenleg egyetlen közvetlen táblát használó, allekérdezés nélküli SELECT esetén támogatott. Az indexleltár ettől függetlenül megtekinthető."));
            return observations;
        }
        var relation = select!.FromClause[0].RangeVar;
        if (relation.Relname != table.Table || !string.IsNullOrEmpty(relation.Schemaname) && relation.Schemaname != table.Schema) return observations;
        bool ColumnMatches(ColumnRef column) => column.Fields.All(f => f.String is not null) &&
            (column.Fields.Count == 1 || column.Fields.Count == 2 && column.Fields[0].String.Sval == (relation.Alias?.Aliasname ?? relation.Relname) ||
             column.Fields.Count == 3 && relation.Alias is null && column.Fields[0].String.Sval == table.Schema && column.Fields[1].String.Sval == table.Table);
        var functions = new HashSet<(string Function, string Column)>();
        if (select.WhereClause is not null) new PostgreSqlAstWalker().Walk(select.WhereClause, 1, entry =>
        {
            if (entry.Node is FuncCall f && RewriteAst.Name(f) is "lower" or "upper" && f.Args.Count == 1 && f.Args[0].ColumnRef is { } column && ColumnMatches(column))
                functions.Add((RewriteAst.Name(f), column.Fields.Last().String.Sval));
        });
        foreach (var function in functions)
        {
            var matches = table.Indexes.Where(i => i.IsUsable && i.Elements.Where(e => !e.Included).OrderBy(e => e.Position).FirstOrDefault() is { Column: null } key && FunctionMatches(key.Definition, function.Function, function.Column)).ToList();
            observations.Add(new($"Függvényes szűrés: {function.Function}({function.Column})", CandidateText(matches, named)));
        }
        if (select.SortClause.Count > 0)
        {
            var orders = select.SortClause.Select(n => n.SortBy).ToList();
            if (orders.All(o => o?.Node?.ColumnRef is { } c && ColumnMatches(c) && o.UseOp.Count == 0))
            {
                var candidates = table.Indexes.Where(i => i.IsUsable && i.Method == "btree" && new[] { false, true }.Any(reverse =>
                {
                    var keys = i.Elements.Where(e => !e.Included).OrderBy(e => e.Position).ToList();
                    if (keys.Count < orders.Count) return false;
                    for (int j = 0; j < orders.Count; j++)
                    {
                        var order = orders[j];
                        bool desc = order.SortbyDir == SortByDir.SortbyDesc;
                        bool nullsFirst = order.SortbyNulls == SortByNulls.First || order.SortbyNulls == SortByNulls.Default && desc;
                        if (keys[j].Column != order.Node.ColumnRef.Fields.Last().String.Sval || (keys[j].Descending ^ reverse) != desc || (keys[j].NullsFirst ^ reverse) != nullsFirst) return false;
                    }
                    return true;
                })).ToList();
                observations.Add(new("Rendezéshez lehetséges index", CandidateText(candidates, named)));
            }
            else observations.Add(new("Rendezés", "A rendezési kifejezés automatikus indexillesztése jelenleg nem támogatott."));
        }
        return observations;
    }

    private static string CandidateText(List<DatabaseIndexMetadata> indexes, HashSet<string> named)
    {
        var used = indexes.Where(i => named.Contains(i.Name)).Select(i => i.Name).ToArray();
        string context = used.Length > 0
            ? "A terv már megnevez lehetséges jelöltet: " + string.Join(", ", used) + ". Új index javaslata előtt a meglévő művelet futását vizsgáld meg; a megnevezés nem bizonyítja, hogy ezzel oldotta meg a szűrést vagy rendezést. "
            : indexes.Count > 0 ? "Van meglévő lehetséges index, de a tervben nem azonosítottuk ennek használatát. Vizsgáld meg az alkalmazhatóságát és a tervező választását; a hiányzó azonosítás nem bizonyítja a használat teljes hiányát. " : "";
        return context + (indexes.Count == 0
        ? "Nem találtunk támogatott szerkezeti illeszkedést. Ez nem bizonyítja, hogy új index szükséges: összetett kifejezések és más indexelési lehetőségek kívül eshetnek a vizsgálaton."
        : "Meglévő, szerkezetileg lehetséges jelölt: " + string.Join(", ", indexes.Select(i => i.Name + (i.Predicate is null ? "" : " (részleges index; a feltétel teljesülését külön ellenőrizni kell)"))) +
          ". Új index előtt ezt vizsgáld meg. A típuskonverzió, kolláció, operátorosztály, szelektivitás és a teljes lekérdezés hatását nem igazoltuk; a jelölt nem garantál indexhasználatot vagy rendezésmentes tervet.");
    }

    private static bool FunctionMatches(string expression, string function, string column)
    {
        try
        {
            var node = RewriteAst.Parse("SELECT " + expression).Stmts[0].Stmt.SelectStmt.TargetList[0].ResTarget.Val;
            if (node.FuncCall is not { } f || RewriteAst.Name(f) != function || f.Args.Count != 1) return false;
            var arg = f.Args[0];
            if (arg.TypeCast is { } cast && (RewriteAst.Type(cast.TypeName, "text") || RewriteAst.Type(cast.TypeName, "varchar"))) arg = cast.Arg;
            return arg.ColumnRef is { Fields.Count: 1 } c && c.Fields[0].String?.Sval == column;
        }
        catch (FormatException) { return false; }
    }
    private static IEnumerable<PlanNode> Nodes(PlanNode node) { yield return node; foreach (var child in node.Children) foreach (var item in Nodes(child)) yield return item; }
    private static string? Text(PlanNode node, string key) => node.AdditionalProperties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
