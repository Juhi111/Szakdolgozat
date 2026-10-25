namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class UnionDeduplicationOccurrence
    {
        public string Location { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}