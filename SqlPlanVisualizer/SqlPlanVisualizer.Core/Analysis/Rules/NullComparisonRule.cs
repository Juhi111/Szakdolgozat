using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class NullComparisonRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.NullComparisons)
            {
                string suggestedOperator = occurrence.Operator == "=" ? "IS NULL" : "IS NOT NULL";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "NULL_COMPARISON",
                    Title = "NULL vizsgálata nem megfelelő összehasonlítással",
                    Description = $"A feltétel {occurrence.Operator} operátorral hasonlít NULL értékhez. A szokásos SQL-szabályok szerint ez ismeretlen " +
                        $"(UNKNOWN) eredményt ad, így nem alkalmas a hiányzó értékek közvetlen vizsgálatára. Hely: {occurrence.Location}.",
                    Recommendation = $"A hiányzó vagy nem hiányzó értékek vizsgálatához használd a {suggestedOperator} feltételt. Ellenőrizd a teljes logikai " +
                        "kifejezést is, hogy a módosítás a kívánt sorokat válassza ki.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
