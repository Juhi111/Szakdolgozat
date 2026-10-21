namespace SqlPlanVisualizer.Core.Models
{
    public class SqlStatementSummary
    {
        public int StatementIndex { get; set; }
        public string StatementType { get; set; } = string.Empty;
        public bool HasWildcardProjection => WildcardProjections.Count > 0;
        public List<WildcardProjectionOccurrence> WildcardProjections { get; set; } = new();
        public List<DistinctOccurrence> DistinctOccurrences { get; } = new();
    }
}
