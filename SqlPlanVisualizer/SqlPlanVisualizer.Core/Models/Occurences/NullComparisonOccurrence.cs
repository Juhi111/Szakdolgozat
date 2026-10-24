using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class NullComparisonOccurrence
    {
        public string Location { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}
