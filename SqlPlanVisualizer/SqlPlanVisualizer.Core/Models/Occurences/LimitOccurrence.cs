using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class LimitOccurrence
    {
        public long RowLimit { get; set; }
        public bool HasOrderBy { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}
