using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class SelectStarRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement) {
            if (statement == null)
            {
                throw new ArgumentNullException(nameof(statement));
            }

            List<SqlAnalysisFinding> findings = new();

            foreach (var wildcardProjection in statement.WildcardProjections)
            {
                string projection = wildcardProjection.Qualifier == null ? "*" : $"{wildcardProjection.Qualifier}.*";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "SELECT_STAR",
                    Title = "Csillagos oszlopkiválasztás",
                    Description = $"Hely: {wildcardProjection.Location}. " +
                       $"Csillagos oszlopkiválasztás: {projection}.",
                    Recommendation = "Vizsgáld meg, hogy szükség van-e minden kiválasztott oszlopra."
                });
            }
            return findings;
        }
    }
}
