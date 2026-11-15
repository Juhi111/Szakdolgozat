using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class LimitWithoutOrderRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.LimitOccurrences)
            {
                if (occurrence.HasOrderBy)
                    continue;

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "LIMIT_WITHOUT_ORDER",
                    Title = "A sorlimithez nincs meghatározott sorrend",
                    Description = $"A lekérdezés legfeljebb {occurrence.RowLimit} sort kér, de nem ad meg ORDER BY rendezést. Emiatt nem garantált, hogy " +
                        $"ismételt futtatáskor ugyanazok a sorok kerülnek az eredménybe. Hely: {occurrence.Location}.",
                    Recommendation = "Ha számít, mely sorokat kapod vissza, adj meg ORDER BY rendezést. Az azonos rendezési értékű sorok sorrendjét szükség " +
                        "esetén egyedi azonosítóval tedd egyértelművé. Ha bármelyik sor megfelel, nincs szükség módosításra; ez elsősorban az " +
                        "eredmény kiszámíthatóságáról szól.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
