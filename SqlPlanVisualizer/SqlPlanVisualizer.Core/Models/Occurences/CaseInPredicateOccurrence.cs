namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class CaseInPredicateOccurrence
    {
        public int SelectorParameterNumber { get; set; }

        public int ComparisonParameterNumber { get; set; }

        public string ThenColumn { get; set; } = string.Empty;

        public string ElseColumn { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}