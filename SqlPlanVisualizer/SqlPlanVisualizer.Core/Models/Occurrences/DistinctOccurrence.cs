namespace SqlPlanVisualizer.Core.Models.Occurrences
{
    public class DistinctOccurrence
    {
        public string Location { get; set; } = string.Empty;
        public bool IsDistinctOn { get; set; }
        public bool HasJoin { get; set; }
        public int? SourceOffset { get; set; }
    }
}
