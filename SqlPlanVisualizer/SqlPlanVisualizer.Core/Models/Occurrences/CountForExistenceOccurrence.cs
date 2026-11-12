namespace SqlPlanVisualizer.Core.Models.Occurrences
{
    public class CountForExistenceOccurrence
    {
        public string Location { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}