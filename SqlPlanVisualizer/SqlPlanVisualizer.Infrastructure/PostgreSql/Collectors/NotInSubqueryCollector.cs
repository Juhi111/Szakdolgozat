using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class NotInSubqueryCollector : CollectorBase<BoolExpr>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(
            BoolExpr expression,
            AstVisitContext context,
            SqlStatementSummary summary)
        {            
            if (expression.Boolop != BoolExprType.NotExpr || expression.Args.Count != 1)            
                return;
            
            var subLink = expression.Args[0].SubLink;

            if (subLink is null || subLink.SubLinkType != SubLinkType.AnySublink)            
                return;
            
            if (subLink.OperName.Count != 0)
                return;
            
            if (subLink.Subselect?.SelectStmt is null)
                return;
            
            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)            
                return;
            
            var column = subLink.Testexpr?.ColumnRef;

            if (column is null)
                return;

            if (!AstColumnReferenceReader.TryRead(column, out string columnName, out string? qualifier))            
                return;
            
            int? sourceOffset = expression.Location >= 0 ? expression.Location : subLink.Location >= 0 ? subLink.Location : null;

            summary.NotInSubqueryOccurrences.Add(
                new NotInSubqueryOccurrence
                {
                    ColumnName = columnName,
                    Qualifier = qualifier,
                    Location = pathFormatter.Format(context),
                    SourceOffset = sourceOffset
                }
            );
        }
    }
}