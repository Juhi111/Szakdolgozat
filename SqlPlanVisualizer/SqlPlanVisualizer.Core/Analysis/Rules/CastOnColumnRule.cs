using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class CastOnColumnRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.CastOnColumnOccurrences)
            {
                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,

                    RuleId = "CAST_ON_COLUMN",

                    Title = "Oszlop típuskonverziója szűrési feltételben",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A(z) {columnReference} oszlop explicit "
                        + "típuskonverziója WHERE vagy JOIN ON "
                        + "környezetben szerepel. "
                        + $"A parser szerinti céltípus neve: "
                        + $"{occurrence.TargetTypeName}. "
                        + "A konverzió befolyásolhatja az eredeti "
                        + "oszlop indexének felhasználhatóságát, "
                        + "de önmagában nem bizonyít lassulást.",

                    Recommendation =
                        "Ellenőrizd a végrehajtási tervet "
                        + "EXPLAIN (ANALYZE, BUFFERS) segítségével. "
                        + "Vizsgáld meg, hogy a feltétel megfogalmazható-e "
                        + "az oszlop eredeti típusával, szükség esetén "
                        + "a konstans vagy paraméter megfelelő típusú "
                        + "megadásával. Ez nem automatikus átírás: "
                        + "az eredménynek és a konverziós hibák "
                        + "viselkedésének is megfelelőnek kell maradnia. "
                        + "Ha az oszlop konverziója szükséges és gyakori, "
                        + "vizsgáld meg a teljes keresési kifejezéshez "
                        + "illeszkedő kifejezésindex lehetőségét.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}