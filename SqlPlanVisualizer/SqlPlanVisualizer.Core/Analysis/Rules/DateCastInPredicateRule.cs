using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class DateCastInPredicateRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.CastOnColumnOccurrences)
            {
                if (!occurrence.IsDateCast)
                    continue;

                string columnReference = string.IsNullOrEmpty(occurrence.Qualifier) ? occurrence.ColumnName : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "DATE_CAST_IN_PREDICATE",
                    Title = "Oszlop dátumra alakítása szűrésben",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A(z) {columnReference} oszlop date típusra "
                        + "alakítása WHERE vagy JOIN ON környezetben "
                        + "szerepel. A konverzió befolyásolhatja "
                        + "az eredeti oszlop indexének felhasználhatóságát. "
                        + "Az elemző az oszlop eredeti típusát nem ismeri.",

                    Recommendation =
                        "Ha időbélyeg egy adott naphoz tartozását "
                        + "vizsgálod, mérlegeld az eredeti oszlopon "
                        + "megadott, a nap kezdetétől a következő nap "
                        + "kezdetéig tartó félig nyílt tartományt "
                        + "(>= alsó határ és < felső határ). "
                        + "A határok típusát és időzónáját az oszlophoz "
                        + "és a kívánt naptári naphoz kell igazítani; "
                        + "egy helyi nap nem feltétlenül 24 óra. "
                        + "Ez nem általános, automatikus átírás: "
                        + "más eredeti típus vagy más feltétel esetén "
                        + "külön ellenőrizd az egyenértékűséget. "
                        + "A tényleges hatást EXPLAIN (ANALYZE, BUFFERS) "
                        + "segítségével vizsgáld meg.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}