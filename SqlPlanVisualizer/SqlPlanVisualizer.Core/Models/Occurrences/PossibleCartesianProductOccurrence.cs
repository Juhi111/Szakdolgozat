using System.Collections.Generic;

namespace SqlPlanVisualizer.Core.Models.Occurrences
{
    public class PossibleCartesianProductOccurrence
    {
        public List<string> Relations { get; set; } = new();
        public string Location { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}