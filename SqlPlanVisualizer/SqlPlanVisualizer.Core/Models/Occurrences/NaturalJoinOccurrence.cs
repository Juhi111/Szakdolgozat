namespace SqlPlanVisualizer.Core.Models.Occurrences
{
    public class NaturalJoinOccurrence
    {
        public string Location { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}