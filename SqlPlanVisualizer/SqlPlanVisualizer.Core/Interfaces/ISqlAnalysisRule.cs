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
        /// <summary>
        /// Alkalmazza a szabályt a megadott SQL-utasítás összefoglalójára.
        /// </summary>
        /// <param name="statement">Az elemzendő utasítás összefoglalója.</param>
        /// <returns>A szabály találatai, vagy üres lista, ha nincs találat.</returns>
        List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement);
    }
}
