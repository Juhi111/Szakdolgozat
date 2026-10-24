using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "LIMIT rendezés nélkül",
                    Description = $"Hely: {occurrence.Location}. A SELECT legfeljebb {occurrence.RowLimit} sort kér, de nem ad meg ORDER BY rendezést.",
                    Recommendation ="Ha fontos, hogy kiszámíthatóan mely sorok kerüljenek az eredménybe, adj meg ORDER BY rendezést, és szükség esetén egyedi holtversenykulcsot. Ha tetszőleges sorok megfelelnek, nem szükséges módosítás.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}