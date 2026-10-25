using System.Linq;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class RowFilterInHavingCollector : CollectorBase<A_Expr>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
        {
            if (expression.Kind != A_Expr_Kind.AexprOp || expression.Name.Count != 1)
                return;

            string? operatorName = expression.Name[0].String?.Sval;

            if (operatorName is not ("=" or "<>" or "!=" or "<" or "<=" or ">" or ">="))
                return;

            var select = FindHavingSelect(context);

            if (select is null || select.Op != SetOperation.SetopNone || select.GroupClause.Count == 0)
                return;

            if (select.GroupClause.Any(item => item.ColumnRef is null))
                return;

            ColumnRef column;
            long constantValue;
            bool isColumnOnLeft;

            if (expression.Lexpr?.ColumnRef is ColumnRef leftColumn &&
                TryReadInteger(expression.Rexpr, out constantValue))
            {
                column = leftColumn;
                isColumnOnLeft = true;
            }
            else if (expression.Rexpr?.ColumnRef is ColumnRef rightColumn &&
                     TryReadInteger(expression.Lexpr, out constantValue))
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

            if (!select.GroupClause.Any(item => SameColumnReference(column, item.ColumnRef)))
                return;

            summary.RowFiltersInHaving.Add(new RowFilterInHavingOccurrence
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

        private static SelectStmt? FindHavingSelect(AstVisitContext context)
        {
            var current = context;

            while (current.Parent != null)
            {
                var parent = current.Parent;

                if (parent.Node is SelectStmt select)
                    return current.FieldName == "having_clause" ? select : null;

                if (parent.Node is Node)
                {
                    current = parent;
                    continue;
                }

                if (parent.Node is BoolExpr boolean &&
                    boolean.Boolop == BoolExprType.AndExpr && current.FieldName == "args")
                {
                    current = parent;
                    continue;
                }

                return null;
            }

            return null;
        }

        private static bool TryReadInteger(Node? node, out long value)
        {
            value = 0;
            var constant = node?.AConst;

            if (constant is null || constant.Isnull || constant.Ival is null)
                return false;

            value = constant.Ival.Ival;
            return true;
        }

        private static bool SameColumnReference(ColumnRef first, ColumnRef second)
        {
            if (first.Fields.Count != second.Fields.Count)
                return false;

            for (int i = 0; i < first.Fields.Count; i++)
            {
                var firstPart = first.Fields[i].String;
                var secondPart = second.Fields[i].String;

                if (firstPart is null || secondPart is null || firstPart.Sval != secondPart.Sval)
                    return false;
            }

            return true;
        }
    }
}