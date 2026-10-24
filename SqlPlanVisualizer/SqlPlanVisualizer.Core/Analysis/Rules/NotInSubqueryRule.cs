using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class NotInSubqueryRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.NotInSubqueryOccurrences)
            {
                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,

                    RuleId = "NOT_IN_SUBQUERY",

                    Title = "NOT IN allekérdezés: ellenőrizendő NULL-kezelés",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A(z) {columnReference} oszlop NOT IN "
                        + "allekérdezésben szerepel. "
                        + "Ha az allekérdezés eredménye NULL értéket "
                        + "tartalmaz, a nem egyező külső értékekre "
                        + "a feltétel nem igaz, hanem ismeretlen lesz. "
                        + "Ezek a sorok ezért nem teljesítik "
                        + "a WHERE vagy JOIN ON feltételt.",

                    Recommendation =
                        "Ellenőrizd, hogy az allekérdezés által "
                        + "visszaadott érték lehet-e NULL. "
                        + "Ha a NULL-mentességet megfelelő megszorítás "
                        + "vagy szűrés már biztosítja, emiatt "
                        + "nem szükséges módosítás. "
                        + "Ha a cél a kapcsolódó sorok hiányának "
                        + "vizsgálata, fontold meg a megfelelő "
                        + "korrelált NOT EXISTS használatát. "
                        + "Ez nem automatikusan egyenértékű átírás: "
                        + "a külső NULL-értékek kívánt kezelését "
                        + "is ellenőrizni kell. "
                        + "Az elemző jelenleg nem ellenőrzi "
                        + "a séma megszorításait, és nem bizonyítja "
                        + "az allekérdezés NULL-mentességét.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}