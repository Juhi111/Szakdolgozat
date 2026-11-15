using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                    Title = "Szöveggé alakított értékre szűrünk",

                    Description = $"A szűrés a(z) {columnReference} oszlop text típusra alakított értékét használja. A szöveges összehasonlítás eltérhet az " +
                        "eredeti típus összehasonlításától, és az indexhasználatot is befolyásolhatja. Az elemző az eredeti oszloptípust nem " +
                        $"ismeri. Hely: {occurrence.Location}.",

                    Recommendation = "Ha az eredeti értékre keresel, vizsgáld meg az oszlop típusának megfelelő paraméter használatát. A szöveges formátum, " +
                        "az összehasonlítási szabályok és a konverziós hibák miatt az átírás nem mindig egyenértékű. Ha kifejezetten a szöveges " +
                        "megjelenés a keresés célja, a konverzió indokolt lehet. A teljesítményhatást végrehajtási tervvel ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
