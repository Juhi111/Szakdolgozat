using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class NullComparisonCollector : CollectorBase<A_Expr>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
        {
            if (expression.Kind != A_Expr_Kind.AexprOp)
                return;

            if (expression.Name.Count != 1)
                return;

            string? operatorName = expression.Name[0].String?.Sval;

            if (operatorName != "=" && operatorName != "<>" && operatorName != "!=")            
                return;            

            bool leftIsNull = expression.Lexpr?.AConst?.Isnull == true;

            bool rightIsNull = expression.Rexpr?.AConst?.Isnull == true;

            if (!leftIsNull && !rightIsNull)
                return;

            summary.NullComparisons.Add(new NullComparisonOccurrence
            {
                Location = pathFormatter.Format(context),
                Operator = operatorName,
                SourceOffset = expression.Location >= 0 ? expression.Location : null
            });
        }
    }
}