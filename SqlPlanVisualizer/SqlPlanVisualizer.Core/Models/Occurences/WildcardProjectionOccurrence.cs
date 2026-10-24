using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class WildcardProjectionOccurrence
    {
        public string? Location { get; set; }
        public string? Qualifier { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}
