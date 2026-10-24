using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class FunctionOnColumnOccurrence
    {
        public string FunctionName { get; set; } = string.Empty;

        public string ColumnName { get; set; } = string.Empty;

        public string? Qualifier { get; set; }

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}
