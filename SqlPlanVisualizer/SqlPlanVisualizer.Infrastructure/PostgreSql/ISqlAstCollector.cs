using SqlPlanVisualizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public interface ISqlAstCollector
    {
        void Collect(AstVisitEntry entry, SqlStatementSummary summary);
    }
}
