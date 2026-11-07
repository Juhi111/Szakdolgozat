namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// A felhasználó által összetartozónak jelölt SQL-utasítást és végrehajtási tervet kapcsolja össze.
    /// </summary>
    public class StatementPlanPair
    {
        public StatementPlanPair(SqlStatementSummary statement, ExecutionPlan plan)
        {
            ArgumentNullException.ThrowIfNull(statement);
            ArgumentNullException.ThrowIfNull(plan);

            Statement = statement;
            Plan = plan;
        }

        public SqlStatementSummary Statement { get; }
        public ExecutionPlan Plan { get; }
    }
}