namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Az SQL-szöveg feldolgozásának eredményét tárolja:
    /// az utasítások számát, összefoglalóit és az esetleges hibaüzenetet.
    /// A feldolgozást sikeresnek jelzi, ha nincs hibaüzenet.
    /// </summary>
    public class SqlParseSummary
    {
        public int StatementCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccess => ErrorMessage is null;
        public List<SqlStatementSummary> Statements { get; set; } = new();
    }
}
