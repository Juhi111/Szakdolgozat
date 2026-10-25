using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class NotInWithNullLiteralCollector : CollectorBase<A_Expr>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
        {            
            if (expression.Kind != A_Expr_Kind.AexprIn)
                return;

            if (expression.Name.Count != 1 || expression.Name[0].String?.Sval != "<>")
                return;

            var items = expression.Rexpr?.List?.Items;

            if (items is null)
                return;

            foreach (var item in items)
            {
                var constant = item.AConst;
                
                if (constant is null || !constant.Isnull)
                    continue;

                summary.NotInWithNullLiterals.Add(new NotInWithNullLiteralOccurrence
                {
                    Location = pathFormatter.Format(context),
                    SourceOffset = constant.Location >= 0
                        ? constant.Location
                        : expression.Location >= 0 ? expression.Location : null
                });                
                break;
            }
        }
    }
}