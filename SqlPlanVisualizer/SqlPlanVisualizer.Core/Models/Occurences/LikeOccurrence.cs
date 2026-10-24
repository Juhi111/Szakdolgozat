using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models.Occurences
{
    public class LikeOccurrence
    {
        public string Pattern { get; set; } = string.Empty;
        public bool IsCaseInsensitive { get; set; }

        public bool HasWildcard { get; set; }
        public bool HasLeadingWildcard { get; set; }

        public string Location { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}
