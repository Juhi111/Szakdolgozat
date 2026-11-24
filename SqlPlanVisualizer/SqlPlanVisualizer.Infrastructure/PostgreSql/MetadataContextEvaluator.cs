using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

public static class MetadataContextEvaluator
{
    public static IEnumerable<IndexObservation> Evaluate(DatabaseTableMetadata table, CombinedAnalysisResult result)
    {
        var ids = result.SqlFindings.Select(f => f.RuleId).Concat(result.PlanFindings.Select(f => f.RuleId)).ToHashSet();
        if (ids.Contains("ROW_ESTIMATE_MISMATCH"))
            yield return new("A becslés ellenőrzéséhez", table.Statistics.Description + " A statisztika kora önmagában nem igazol pontatlan becslést. LIMIT mellett a korai leállást is ellenőrizd.");
        if (ids.Contains("HIGH_HEAP_FETCHES"))
            yield return new("A táblához visszaolvasás háttere", table.Statistics.Description + $" Katalógus szerinti látható lapok: {table.Statistics.AllVisiblePages}/{table.Statistics.Pages}. Ez becslés, nem a visibility map élő mérése; VACUUM szükségességét nem bizonyítja.");
        if (ids.Any(id => id.Contains("SPILL") || id is "HASH_MULTIPLE_BATCHES" or "LOSSY_BITMAP_HEAP_SCAN"))
            yield return new("Memória és lemezhasználat", $"A metaadat-kapcsolat work_mem értéke: {Setting(table, "work_mem")}; hash_mem_multiplier: {Setting(table, "hash_mem_multiplier")}. " +
                "A teljes memóriaigény több művelet és dolgozó között összeadódhat. Először a feldolgozott adatmennyiséget vizsgáld meg; ne emeld automatikusan a globális beállítást. A terv lekérésekor más érték is érvényben lehetett.");
        if (ids.Contains("PARALLEL_WORKER_SHORTAGE"))
            yield return new("Párhuzamos feldolgozás beállításai", $"Műveletenkénti korlát: {Setting(table, "max_parallel_workers_per_gather")}; párhuzamos dolgozók felső korlátja: {Setting(table, "max_parallel_workers")}. Ezek nem az éppen szabad dolgozók számát mutatják.");
        if (ids.Contains("LEADING_WILDCARD_SEARCH"))
            yield return new("Szöveges keresés lehetőségei", table.Extensions.Contains("pg_trgm")
                ? "A pg_trgm bővítmény telepítve van. Ez önmagában nem jelent megfelelő trigram indexet; az indexdefiníciót és a tervet is ellenőrizd."
                : "A pg_trgm bővítményt nem találtuk a telepített bővítmények között. Részszöveges keresésnél megvizsgálható, de nem minden kereséshez szükséges.");
        if (ids.Overlaps(new[] { "NOT_IN_SUBQUERY", "DISTINCT_USAGE", "UNION_DEDUPLICATION", "COUNT_FOR_EXISTENCE" }))
            yield return new("NULL és egyediség", "Az oszlopok nullázhatóságát, valamint az elsődleges, egyedi és idegen kulcsokat lekértük. " +
                "Ezek a részletek között láthatók. JOIN és allekérdezés módosíthatja az eredmény nullázhatóságát vagy egyediségét, ezért ezek alapján nem végzünk automatikus törlést vagy átírást.");
    }
    private static string Setting(DatabaseTableMetadata table, string key) => table.Settings.GetValueOrDefault(key) ?? "nem elérhető";
}
