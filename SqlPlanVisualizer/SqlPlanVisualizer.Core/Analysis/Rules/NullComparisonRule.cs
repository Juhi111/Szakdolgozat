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
                    Title = "NULL összehasonlítása hagyományos operátorral",
                    Description =$"Hely: {occurrence.Location}. Az összehasonlítás {occurrence.Operator} operátort használ NULL értékkel. A szokásos SQL-szemantika szerint ennek eredménye UNKNOWN, nem igaz vagy hamis.",
                    Recommendation =$"Ha a cél a hiányzó vagy nem hiányzó értékek vizsgálata, használd a {suggestedOperator} feltételt.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
