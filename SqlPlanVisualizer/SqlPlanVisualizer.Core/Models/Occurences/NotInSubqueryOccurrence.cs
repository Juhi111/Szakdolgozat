namespace SqlPlanVisualizer.Core.Models
{
    public class NotInSubqueryOccurrence
    {
        public string ColumnName { get; set; } = string.Empty;

        public string? Qualifier { get; set; }

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}