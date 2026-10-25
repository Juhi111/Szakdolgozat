using System.Collections.Generic;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class ConcatenationCollector : CollectorBase<A_Expr>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
        {
            if (!IsConcatenation(expression))
                return;

            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)            
                return;
           
            if (context.Parent?.Parent?.Node is A_Expr parentExpression && IsConcatenation(parentExpression) && (ReferenceEquals(parentExpression.Lexpr?.AExpr, expression) || ReferenceEquals(parentExpression.Rexpr?.AExpr, expression)))            
                return;            

            if (!TryReadColumns(expression, out var columns))
                return;

            summary.ConcatenationOccurrences.Add(
                new ConcatenationOccurrence
                {
                    ColumnReferences = columns,
                    Location = pathFormatter.Format(context),
                    SourceOffset = expression.Location >= 0 ? expression.Location : null
                });
        }

        private static bool IsConcatenation(A_Expr expression)
        {
            return expression.Kind == A_Expr_Kind.AexprOp && expression.Name.Count == 1 && expression.Name[0].String?.Sval == "||";
        }

        private static bool TryReadColumns(A_Expr expression, out List<string> columns)
        {
            columns = new List<string>();

            if (expression.Lexpr is null || expression.Rexpr is null)
                return false;

            var pending = new Stack<Node>();

            pending.Push(expression.Rexpr);
            pending.Push(expression.Lexpr);

            while (pending.Count > 0)
            {
                var node = pending.Pop();

                if (node.AExpr is A_Expr nested && IsConcatenation(nested))
                {
                    if (nested.Lexpr is null || nested.Rexpr is null)
                        return false;

                    pending.Push(nested.Rexpr);
                    pending.Push(nested.Lexpr);
                    continue;
                }

                if (node.ColumnRef is ColumnRef column)
                {
                    if (!AstColumnReferenceReader.TryRead(column, out string columnName, out string? qualifier))
                    {
                        return false;
                    }

                    string reference = string.IsNullOrEmpty(qualifier) ? columnName : $"{qualifier}.{columnName}";

                    columns.Add(reference);
                    continue;
                }

                var constant = node.AConst;

                if (constant != null && !constant.Isnull && constant.Sval != null)                
                    continue;
              
                return false;
            }

            return columns.Count > 0;
        }
    }
}