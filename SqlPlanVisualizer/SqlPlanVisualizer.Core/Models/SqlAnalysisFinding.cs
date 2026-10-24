namespace SqlPlanVisualizer.Core.Models
{
    public class SqlAnalysisFinding
    {
        public int StatementIndex { get; set; }
        public string RuleId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}
