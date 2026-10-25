using System;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class YearExtractionCollector : CollectorBase<FuncCall>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(FuncCall function, AstVisitContext context, SqlStatementSummary summary)
        {
            var clause = contextResolver.GetClause(context);

            if (clause is not (AstClause.Where or AstClause.JoinOn))
                return;

            if (!TryReadFunctionName(function, out string functionName) || function.Args.Count != 2)
                return;

            var datePart = function.Args[0].AConst;

            if (datePart is null || datePart.Isnull ||
                !string.Equals(datePart.Sval?.Sval, "year", StringComparison.OrdinalIgnoreCase))
                return;

            var column = function.Args[1].ColumnRef;

            if (column is null || !AstColumnReferenceReader.TryRead(column, out string columnName, out string? qualifier))
                return;

            if (!TryReadComparison(function, context, out string comparisonOperator, out bool isExtractionOnLeft))
                return;

            summary.YearExtractionOccurrences.Add(new YearExtractionOccurrence
            {
                FunctionName = functionName,
                ColumnName = columnName,
                Qualifier = qualifier,
                ComparisonOperator = comparisonOperator,
                IsExtractionOnLeft = isExtractionOnLeft,
                Location = pathFormatter.Format(context),
                SourceOffset = function.Location >= 0 ? function.Location : null
            });
        }

        private static bool TryReadFunctionName(FuncCall function, out string functionName)
        {
            functionName = string.Empty;

            string? name = function.Funcname.Count switch
            {
                1 => function.Funcname[0].String?.Sval,
                2 when function.Funcname[0].String?.Sval == "pg_catalog"
                    => function.Funcname[1].String?.Sval,
                _ => null
            };

            if (name is not ("extract" or "date_part"))
                return false;

            functionName = name;
            return true;
        }

        private static bool TryReadComparison(FuncCall function, AstVisitContext context, out string comparisonOperator, out bool isExtractionOnLeft)
        {
            comparisonOperator = string.Empty;
            isExtractionOnLeft = false;
            
            if (context.Parent?.Parent?.Node is not A_Expr comparison)
                return false;

            if (comparison.Kind != A_Expr_Kind.AexprOp || comparison.Name.Count != 1)
                return false;

            string? operatorName = comparison.Name[0].String?.Sval;

            if (operatorName is not ("=" or "<>" or "!=" or "<" or "<=" or ">" or ">="))
                return false;

            if (comparison.Lexpr is null || comparison.Rexpr is null)
                return false;

            isExtractionOnLeft = ReferenceEquals(comparison.Lexpr.FuncCall, function);

            if (!isExtractionOnLeft && !ReferenceEquals(comparison.Rexpr.FuncCall, function))
                return false;

            comparisonOperator = operatorName;
            return true;
        }
    }
}