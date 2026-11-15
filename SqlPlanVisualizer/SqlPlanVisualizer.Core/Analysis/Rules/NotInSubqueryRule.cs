using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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

                    Title = "A NOT IN eredményét NULL is befolyásolhatja",

                    Description = $"A(z) {columnReference} értékét NOT IN feltétel hasonlítja az allekérdezés eredményéhez. Ha abban NULL is van, egy nem " +
                        "egyező külső értékre a feltétel ismeretlen lesz az elvárt igaz helyett. Ez a sor nem teljesíti a szűrési vagy " +
                        $"kapcsolási feltételt. Hely: {occurrence.Location}.",

                    Recommendation = "Ellenőrizd, visszaadhat-e NULL-t az allekérdezés; ezt az elemző sémaadatok nélkül nem tudja bizonyítani. Ha a cél a " +
                        "kapcsolódó sor hiányának vizsgálata, mérlegeld a NOT EXISTS használatát. Az átírásnál a külső NULL-értékek kezelését is " +
                        "ellenőrizd, mert a két megoldás nem mindig egyenértékű. Biztosított NULL-mentességnél emiatt nem szükséges módosítás.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
