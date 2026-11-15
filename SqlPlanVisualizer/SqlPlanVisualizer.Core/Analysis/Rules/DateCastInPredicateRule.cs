using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                    Title = "Dátummá alakított oszlopra szűrünk",

                    Description = $"A szűrés a(z) {columnReference} oszlop date típusra alakított értékét használja. Ez megnehezítheti az eredeti oszlop " +
                        $"indexének használatát. Az SQL szövegéből az oszlop eredeti típusa nem állapítható meg. Hely: {occurrence.Location}.",

                    Recommendation = "Ha időbélyegeket keresel egy adott napra, vizsgáld meg a tartományos feltételt: az érték legyen legalább a nap kezdete, " +
                        "és kisebb a következő nap kezdeténél. A határokat az oszlop típusához és a kívánt időzónához igazítsd; egy helyi nap " +
                        "nem mindig 24 óra. Más típusnál vagy összehasonlításnál külön ellenőrizd az átírás helyességét. A gyorsulást a " +
                        "végrehajtási terv alapján értékeld.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
