namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class YearExtractionOccurrence
    {
        public string FunctionName { get; set; } = string.Empty;

        public string ColumnName { get; set; } = string.Empty;

        public string? Qualifier { get; set; }

        public string ComparisonOperator { get; set; } = string.Empty;

        public bool IsExtractionOnLeft { get; set; }

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}