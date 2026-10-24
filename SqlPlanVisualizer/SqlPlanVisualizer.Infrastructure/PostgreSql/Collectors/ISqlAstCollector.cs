using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors
{
    public interface ISqlAstCollector
    {
        void Collect(AstVisitEntry entry, SqlStatementSummary summary);
    }
}
