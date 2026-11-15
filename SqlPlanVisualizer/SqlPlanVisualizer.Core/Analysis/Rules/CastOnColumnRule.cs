using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                if (occurrence.IsDateCast || occurrence.IsTextCast)
                    continue;

                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,

                    RuleId = "CAST_ON_COLUMN",

                    Title = "Szűrés más típusra alakított oszlopra",

                    Description = $"A szűrés a(z) {columnReference} oszlop {occurrence.TargetTypeName} típusra alakított értékét használja. Ez " +
                        $"befolyásolhatja, hogy az eredeti oszlop indexe segíti-e a keresést. Hely: {occurrence.Location}.",

                    Recommendation = "Ellenőrizd, megadható-e a keresett érték az oszlop eredeti típusának megfelelő paraméterként vagy konstansként. Az " +
                        "átíráskor az eredmény és a konverziós hibák kezelése is maradjon megfelelő. Ha az oszlop átalakítása szükséges és a " +
                        "keresés gyakori, vizsgáld meg a teljes kifejezésre készített index lehetőségét. A tényleges előnyt végrehajtási tervvel " +
                        "ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
