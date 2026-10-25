using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class CaseInPredicateCollector : CollectorBase<CaseExpr>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(
            CaseExpr expression,
            AstVisitContext context,
            SqlStatementSummary summary)
        {            
            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)
            {
                return;
            }

            if (expression.Arg != null || expression.Args.Count != 1)
                return;

            var when = expression.Args[0].CaseWhen;

            if (when is null)
                return;
            
            var condition = when.Expr?.AExpr;

            if (!IsEquality(condition))
                return;

            ParamRef? selectorParameter = null;

            if (condition!.Lexpr?.ParamRef is ParamRef leftParameter &&
                IsStringConstant(condition.Rexpr))
            {
                selectorParameter = leftParameter;
            }
            else if (condition.Rexpr?.ParamRef is ParamRef rightParameter && IsStringConstant(condition.Lexpr))
            {
                selectorParameter = rightParameter;
            }

            if (selectorParameter is null || selectorParameter.Number < 1)
            {
                return;
            }
            
            if (!TryReadColumn(when.Result, out string thenColumn) ||
                !TryReadColumn(expression.Defresult, out string elseColumn))
            {
                return;
            }

            var comparison = context.Parent?.Parent?.Node as A_Expr;

            if (!IsEquality(comparison))
                return;

            ParamRef? comparisonParameter;

            if (ReferenceEquals(comparison!.Lexpr?.CaseExpr, expression))
            {
                comparisonParameter = comparison.Rexpr?.ParamRef;
            }
            else if (ReferenceEquals(comparison!.Rexpr?.CaseExpr, expression))
            {
                comparisonParameter = comparison.Lexpr?.ParamRef;
            }
            else
            {
                return;
            }

            if (comparisonParameter is null || comparisonParameter.Number < 1)
            {
                return;
            }

            summary.CaseInPredicateOccurrences.Add(
                new CaseInPredicateOccurrence
                {
                    SelectorParameterNumber = selectorParameter.Number,
                    ComparisonParameterNumber = comparisonParameter.Number,
                    ThenColumn = thenColumn,
                    ElseColumn = elseColumn,
                    Location = pathFormatter.Format(context),

                    SourceOffset = expression.Location >= 0
                        ? expression.Location
                        : null
                });
        }

        private static bool IsEquality(A_Expr? expression)
        {
            return expression != null &&
                   expression.Kind == A_Expr_Kind.AexprOp &&
                   expression.Name.Count == 1 &&
                   expression.Name[0].String?.Sval == "=";
        }

        private static bool IsStringConstant(Node? node)
        {
            var constant = node?.AConst;

            return constant != null &&
                   !constant.Isnull &&
                   constant.Sval != null;
        }

        private static bool TryReadColumn(Node? node, out string reference)
        {
            reference = string.Empty;

            var column = node?.ColumnRef;

            if (column is null)
                return false;

            if (!AstColumnReferenceReader.TryRead(column, out string columnName, out string? qualifier))
            {
                return false;
            }

            reference = string.IsNullOrEmpty(qualifier) ? columnName : $"{qualifier}.{columnName}";

            return true;
        }
    }
}