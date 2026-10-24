using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class LimitCollector : CollectorBase<SelectStmt>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(SelectStmt selectStatement, AstVisitContext context, SqlStatementSummary summary)
        {
            if (context.Parent is null || context.Parent.Parent is not null || context.FieldName != "select_stmt")            
                return;

            var limitConstant = selectStatement.LimitCount?.AConst;

            if (limitConstant is null || limitConstant.Isnull || limitConstant.Ival is null)
                return;

            long rowLimit = limitConstant.Ival.Ival;

            if (rowLimit <= 0)
                return;

            summary.LimitOccurrences.Add(new LimitOccurrence
            {
                RowLimit = rowLimit,
                HasOrderBy = selectStatement.SortClause.Count > 0,
                Location = pathFormatter.Format(context),
                SourceOffset = limitConstant.Location >= 0 ? limitConstant.Location : null
            });
        }
    }
}