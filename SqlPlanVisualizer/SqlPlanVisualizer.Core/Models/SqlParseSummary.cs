using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models
{
    public class SqlParseSummary
    {
        public int StatementCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccess => ErrorMessage is null;
    }
}
