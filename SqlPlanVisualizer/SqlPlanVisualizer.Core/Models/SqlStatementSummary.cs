using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models
{
    public class SqlStatementSummary
    {
        public int StatementIndex { get; set; }
        public string StatementType { get; set; } = string.Empty;
        public bool HasWildcardProjection => WildcardProjections.Count > 0;
        public List<WildcardProjectionOccurrence> WildcardProjections { get; set; } = new();


    }
}
