namespace SqlPlanVisualizer.Core.Models.Occurrences
{
    public class OffsetOccurrence
    {
        public long RowOffset { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}