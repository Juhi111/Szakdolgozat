using SqlPlanVisualizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Interfaces
{
    public interface ISqlAnalysisRule
    {
        List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement);
    }
}
