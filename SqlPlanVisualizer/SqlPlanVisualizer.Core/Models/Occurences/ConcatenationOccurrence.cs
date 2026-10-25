using System.Collections.Generic;

namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class ConcatenationOccurrence
    {
        public List<string> ColumnReferences { get; set; } = new();

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}