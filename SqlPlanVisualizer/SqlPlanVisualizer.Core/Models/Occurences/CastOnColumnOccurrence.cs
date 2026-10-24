namespace SqlPlanVisualizer.Core.Models
{
    public class CastOnColumnOccurrence
    {
        public string ColumnName { get; set; } = string.Empty;

        public string? Qualifier { get; set; }

        public string TargetTypeName { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}