using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class OffsetCollector : CollectorBase<SelectStmt>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(SelectStmt select, AstVisitContext context, SqlStatementSummary summary)
        {
            var constant = select.LimitOffset?.AConst;

            if (constant is null || constant.Isnull || constant.Ival is null)
                return;

            long rowOffset = constant.Ival.Ival;

            if (rowOffset <= 0)
                return;

            summary.OffsetOccurrences.Add(new OffsetOccurrence
            {
                RowOffset = rowOffset,
                Location = pathFormatter.Format(context),
                SourceOffset = constant.Location >= 0 ? constant.Location : null
            });
        }
    }
}