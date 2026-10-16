namespace SqlPlanVisualizer.Core.Models
{
    public class SqlParseSummary
    {
        public int StatementCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccess => ErrorMessage is null;
        public List<SqlStatementSummary> Statements { get; set; } = new();
    }
}
