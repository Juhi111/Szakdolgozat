namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Egy automatikusan azonosított vagy kézzel megerősített SQL–terv párost tárol.
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