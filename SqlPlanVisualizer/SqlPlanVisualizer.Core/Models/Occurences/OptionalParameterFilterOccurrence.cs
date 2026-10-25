namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class OptionalParameterFilterOccurrence
    {
        public int ParameterNumber { get; set; }

        public string ColumnName { get; set; } = string.Empty;

        public string? Qualifier { get; set; }

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}