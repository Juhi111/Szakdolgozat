using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class ArithmeticOnColumnCollector : CollectorBase<A_Expr>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
        {            
            if (expression.Kind != A_Expr_Kind.AexprOp)
                return;

            if (expression.Name.Count != 1)
                return;

            string? operatorName = expression.Name[0].String?.Sval;

            if (operatorName != "+" && operatorName != "-" && operatorName != "*" && operatorName != "/")            
                return;            
            
            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)            
                return;
            
            ColumnRef column;
            long constantValue;
            bool isColumnOnLeft;

            if (expression.Lexpr?.ColumnRef is ColumnRef leftColumn && TryReadIntegerConstant(expression.Rexpr, out constantValue))
            {
                column = leftColumn;
                isColumnOnLeft = true;
            }
            else if (
                expression.Rexpr?.ColumnRef is ColumnRef rightColumn && TryReadIntegerConstant(expression.Lexpr, out constantValue))
            {
                column = rightColumn;
                isColumnOnLeft = false;
            }
            else
            {
                return;
            }
            
            if (!AstColumnReferenceReader.TryRead(column, out string columnName, out string? qualifier))            
                return;
            
            summary.ArithmeticOnColumnOccurrences.Add(
                new ArithmeticOnColumnOccurrence
                {
                    ColumnName = columnName,
                    Qualifier = qualifier,
                    Operator = operatorName,
                    ConstantValue = constantValue,
                    IsColumnOnLeft = isColumnOnLeft,
                    Location = pathFormatter.Format(context),
                    SourceOffset = expression.Location >= 0 ? expression.Location : null
                });
        }

        private static bool TryReadIntegerConstant(Node? node, out long value)
        {
            value = 0;

            var constant = node?.AConst;

            if (constant is null ||
                constant.Isnull ||
                constant.Ival is null)
            {
                return false;
            }

            value = constant.Ival.Ival;
            return true;
        }
    }
}