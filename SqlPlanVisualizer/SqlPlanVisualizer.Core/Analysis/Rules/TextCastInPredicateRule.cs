using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class TextCastInPredicateRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.CastOnColumnOccurrences)
            {
                if (!occurrence.IsTextCast)
                    continue;

                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "TEXT_CAST_IN_PREDICATE",
                    Title = "Oszlop szöveggé alakítása szűrésben",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A(z) {columnReference} oszlop text típusra "
                        + "alakítása WHERE vagy JOIN ON környezetben "
                        + "szerepel. Az elemző az eredeti oszloptípust "
                        + "nem ismeri. A szöveges alak használata "
                        + "befolyásolhatja az összehasonlítás jelentését "
                        + "és az eredeti oszlop indexének "
                        + "felhasználhatóságát.",

                    Recommendation =
                        "Ha az oszlop eredeti értékére szeretnél szűrni, "
                        + "vizsgáld meg, hogy használható-e az eredeti "
                        + "típusnak megfelelő konstans vagy paraméter "
                        + "az oszlop szöveggé alakítása helyett. "
                        + "Ez nem automatikusan egyenértékű átírás: "
                        + "a szöveges formátum, a rendezési szabályok "
                        + "és a konverziós hibák is számítanak. "
                        + "Ha kifejezetten a szöveges alak keresése "
                        + "a cél, a konverzió indokolt lehet. "
                        + "A tényleges hatást EXPLAIN (ANALYZE, BUFFERS) "
                        + "segítségével ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}