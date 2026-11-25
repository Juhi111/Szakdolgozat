using System.Text.Json;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

/// <summary>Átírás nélkül is értelmezi az érintett táblák katalógusadatait. Nem következtet hiányzó adatokból.</summary>
public static class MetadataContextEvaluator
{
    public static IEnumerable<IndexObservation> Evaluate(DatabaseTableMetadata table, CombinedAnalysisResult result)
    {
        var sqlIds = result.SqlFindings.Select(f => f.RuleId).ToHashSet();
        var planIds = result.PlanFindings.Select(f => f.RuleId).ToHashSet();
        var localIds = result.PlanFindings.Where(f => Matches(f.Node, table)).Select(f => f.RuleId).ToHashSet();
        if (localIds.Contains("ROW_ESTIMATE_MISMATCH"))
        {
            yield return new("Mi segít megérteni a sorbecslést?", table.Statistics.Description +
                " A statisztika kora önmagában nem igazol hibás becslést. LIMIT mellett a korai leállást is ellenőrizd.");
            yield return new("Összefüggő oszlopok statisztikái", table.ExtendedStatistics.Count == 0
                ? "Ezen a táblán nem találtunk kiterjesztett statisztikát. Ha több, egymással összefüggő oszlopra szűrsz, érdemes megvizsgálni a szükségességét. Egyetlen eltérő becslés ezt még nem bizonyítja."
                : $"A táblán {table.ExtendedStatistics.Count} kiterjesztett statisztika található. A részletekben ellenőrizhető, hogy a vizsgált feltételek oszlopait írják-e le; puszta létezésük nem garantál pontos becslést.");
        }
        if (localIds.Contains("HIGH_FILTER_REMOVAL"))
            yield return new("Sok beolvasott sor nem jut tovább", $"A(z) {table.DisplayName} táblán {table.Indexes.Count(i => i.IsUsable)} használható állapotú indexet találtunk. " +
                "Ezek nem feltétlenül illeszkednek a szűréshez. A feltétel oszlopait, az indexek vezető kulcsait és a részleges indexek feltételét együtt érdemes vizsgálni; a sok eldobott sor önmagában nem bizonyít hiányzó indexet.");
        if (localIds.Contains("REPEATED_SEQUENTIAL_SCAN"))
            yield return new("Ismétlődő táblabejárás", $"A(z) {table.DisplayName} tábla többszöri bejárását jelezte a terv. " +
                "Nézd meg, melyik külső művelet indítja újra, és milyen kapcsolási feltételt használ. A meglévő indexek és kulcsok a részletekben láthatók; kis táblán az ismételt bejárás is lehet megfelelő.");
        if (localIds.Contains("HIGH_HEAP_FETCHES"))
        {
            var s = table.Statistics;
            string visibility = s.Pages is > 0 && s.AllVisiblePages is >= 0 && s.AllVisiblePages <= s.Pages
                ? $"A katalógus szerint a lapok körülbelül {100d * s.AllVisiblePages.Value / s.Pages.Value:F1}%-a teljesen látható."
                : "A teljesen látható lapok aránya a rendelkezésre álló adatokból nem számolható ki.";
            yield return new("Miért kellett visszaolvasni a táblát?", visibility + " Ez katalógusadat, nem a visibility map élő mérése. " +
                "A közelmúltbeli módosítások és a VACUUM állapota is számíthat; ne indíts karbantartást csak ebből az arányból. " + s.Description);
        }
        // Session settings describe the metadata connection, not a particular scan or the original capture.
        if (planIds.Any(id => id.Contains("SPILL") || id is "HASH_MULTIPLE_BATCHES" or "LOSSY_BITMAP_HEAP_SCAN"))
            yield return new("A memóriahasználat háttere", $"A metaadat-kapcsolat work_mem értéke: {Setting(table, "work_mem")}; hash_mem_multiplier: {Setting(table, "hash_mem_multiplier")}. " +
                "A memóriaigény műveletenként és dolgozónként összeadódhat. Először a feldolgozott adatmennyiséget vizsgáld meg. A terv lekérésekor más beállítás is érvényben lehetett.");
        if (planIds.Contains("PARALLEL_WORKER_SHORTAGE"))
            yield return new("Párhuzamos feldolgozás beállításai", $"Műveletenkénti korlát: {Setting(table, "max_parallel_workers_per_gather")}; párhuzamos dolgozók felső korlátja: {Setting(table, "max_parallel_workers")}. Ezek nem az éppen szabad dolgozók számát mutatják.");
        foreach (var index in table.Indexes.Where(i => !i.IsUsable))
            yield return new("Nem használható állapotú index", $"A(z) {index.Name} index állapota figyelmet igényel: érvényes={index.IsValid}, írásra kész={index.IsReady}, élő={index.IsLive}. " +
                "Folyamatban lévő indexépítés is okozhatja. Ellenőrizd az állapotát, mielőtt új indexet készítenél ugyanarra a célra.");
        if (sqlIds.Overlaps(new[] { "NOT_IN_SUBQUERY", "DISTINCT_USAGE", "UNION_DEDUPLICATION", "COUNT_FOR_EXISTENCE" }))
        {
            var keys = table.Keys.Where(k => k.Kind is "p" or "u").ToArray();
            yield return new("Mit garantálnak a kulcsok?", keys.Length == 0
                ? $"A(z) {table.DisplayName} táblán nem találtunk elsődleges vagy UNIQUE megszorítást. Az egyedi indexeket ettől külön listázzuk. Ebből nem következik, hogy az eredmény ismétlődő sorokat tartalmaz."
                : $"A(z) {table.DisplayName} egyediségi megszorításai: {string.Join("; ", keys.Select(k => $"{k.Name} ({string.Join(", ", k.Columns)}){(k.Deferrable ? " – halasztható" : "")}"))}. " +
                  "Az egyediség a teljes kulcsra vonatkozik. JOIN vagy a kulcs egy részének kiválasztása mellett az eredménysorok ismétlődhetnek.");
            if (sqlIds.Contains("NOT_IN_SUBQUERY"))
                yield return new("NULL-értékek a kizáró feltételben", $"A(z) {table.DisplayName} tábla NULL-t tiltó oszlopai: {Names(table.Columns.Where(c => c.IsNullable == false))}. " +
                    $"NULL-t megengedő oszlopai: {Names(table.Columns.Where(c => c.IsNullable == true))}. " +
                    "A NOT IN mindkét oldalát vizsgálni kell. Kifejezés vagy külső összekapcsolás módosíthatja a nullázhatóságot; ismeretlen adatból nem készítünk átírást.");
        }
        var predicateIds = new HashSet<string>(sqlIds);
        predicateIds.UnionWith(localIds);
        foreach (var observation in PredicateColumns(table, result, predicateIds)) yield return observation;
    }

    private static IEnumerable<IndexObservation> PredicateColumns(DatabaseTableMetadata table, CombinedAnalysisResult result, HashSet<string> ids)
    {
        var sql = result.Pair.Plan.Origin?.SqlText;
        if (string.IsNullOrWhiteSpace(sql)) yield break;
        SelectStmt? select;
        try { var parsed = RewriteAst.Parse(sql); select = parsed.Stmts.Count == 1 ? parsed.Stmts[0].Stmt.SelectStmt : null; }
        catch (FormatException) { yield break; }
        if (!RewriteAst.Supported(select) || select!.WhereClause is null || RewriteAst.Contains(select, n => n is SubLink)) yield break;
        var relation = select.FromClause[0].RangeVar;
        // Do not guess which schema an unqualified name resolves to.
        if (relation.Schemaname != table.Schema || relation.Relname != table.Table || relation.Alias?.Colnames.Count > 0) yield break;
        if (ids.Contains("LEADING_WILDCARD_SEARCH"))
            yield return new("Részszöveges keresés", table.Extensions.Contains("pg_trgm")
                ? "A pg_trgm bővítmény telepítve van. Ez még nem jelent megfelelő trigram indexet: az index definíciójának és a keresési feltételnek is illeszkednie kell."
                : "A pg_trgm bővítményt nem találtuk. Gyakori részszöveges keresésnél megvizsgálható a használata; telepítést és indexkészítést nem végzünk automatikusan.");
        if (!ids.Overlaps(new[] { "HIGH_FILTER_REMOVAL", "FUNCTION_ON_COLUMN", "CAST_ON_COLUMN", "DATE_CAST_IN_PREDICATE", "YEAR_EXTRACTION_IN_PREDICATE", "TEXT_CAST_IN_PREDICATE", "LEADING_WILDCARD_SEARCH", "OPTIONAL_PARAMETER_FILTER", "ARITHMETIC_ON_COLUMN" })) yield break;
        var names = new HashSet<string>();
        new PostgreSqlAstWalker().Walk(select.WhereClause, 1, entry =>
        {
            if (entry.Node is not ColumnRef c || c.Fields.Any(f => f.String is null)) return;
            if (c.Fields.Count == 1 || c.Fields.Count == 2 && c.Fields[0].String.Sval == (relation.Alias?.Aliasname ?? relation.Relname))
                names.Add(c.Fields.Last().String.Sval);
        });
        foreach (var column in table.Columns.Where(c => names.Contains(c.Name)))
        {
            var stats = table.ColumnStatistics.FirstOrDefault(c => c.Column == column.Name && !c.Inherited);
            yield return new($"A szűrés oszlopa: {column.Name}", column.Description + ". " + (stats is null
                ? "Ehhez az oszlophoz nem érhető el statisztikai összefoglaló; ez jogosultság vagy hiányzó statisztika miatt is lehet."
                : $"Statisztikai NULL-arány: {(stats.NullFraction is double fraction ? $"{fraction:P1}" : "ismeretlen")}. Ez mintavételes adat; nem helyettesíti a NOT NULL megszorítást.") +
                " Az oszloptípus és a feltételben szereplő átalakítás segít eldönteni, melyik meglévő index jöhet szóba.");
        }
    }
    private static string Names(IEnumerable<ColumnAnalysisMetadata> columns) => string.Join(", ", columns.Select(c => c.Name)) is { Length: > 0 } text ? text : "nincs ismert oszlop";
    private static bool Matches(PlanNode node, DatabaseTableMetadata table) => node.RelationName == table.Table &&
        node.AdditionalProperties.TryGetValue("Schema", out var schema) && schema.ValueKind == JsonValueKind.String && schema.GetString() == table.Schema;
    private static string Setting(DatabaseTableMetadata table, string key) => table.Settings.GetValueOrDefault(key) ?? "nem elérhető";
}
