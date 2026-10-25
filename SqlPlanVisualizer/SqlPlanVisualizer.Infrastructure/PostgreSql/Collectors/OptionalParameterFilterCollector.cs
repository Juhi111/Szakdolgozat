using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class OptionalParameterFilterCollector : CollectorBase<BoolExpr>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(BoolExpr expression, AstVisitContext context, SqlStatementSummary summary)
        {            
            if (expression.Boolop != BoolExprType.OrExpr || expression.Args.Count != 2)            
                return;
            
            
            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)            
                return;            
            
            if (!TryMatch(expression.Args[0], expression.Args[1], out int parameterNumber, out string columnName, out string? qualifier) && 
                !TryMatch(expression.Args[1], expression.Args[0], out parameterNumber, out columnName, out qualifier))            
                return;
            

            summary.OptionalParameterFilters.Add(
                new OptionalParameterFilterOccurrence
                {
                    ParameterNumber = parameterNumber,
                    ColumnName = columnName,
                    Qualifier = qualifier,
                    Location = pathFormatter.Format(context),
                    SourceOffset = expression.Location >= 0 ? expression.Location
                        : null
                });
        }

        private static bool TryMatch(Node nullBranch, Node comparisonBranch, out int parameterNumber, out string columnName, out string? qualifier)
        {
            parameterNumber = 0;
            columnName = string.Empty;
            qualifier = null;
            
            var nullTest = nullBranch.NullTest;

            if (nullTest is null ||  nullTest.Nulltesttype != NullTestType.IsNull)            
                return false;            

            var nullParameter = nullTest.Arg?.ParamRef;

            if (nullParameter is null || nullParameter.Number < 1)
                return false;
            
            var comparison = comparisonBranch.AExpr;

            if (comparison is null || comparison.Kind != A_Expr_Kind.AexprOp || comparison.Name.Count != 1 || comparison.Name[0].String?.Sval != "=")            
                return false;            

            ColumnRef column;
            ParamRef comparedParameter;
            
            if (comparison.Lexpr?.ColumnRef is ColumnRef leftColumn && comparison.Rexpr?.ParamRef is ParamRef rightParameter)
            {
                column = leftColumn;
                comparedParameter = rightParameter;
            }
            
            else if (comparison.Rexpr?.ColumnRef is ColumnRef rightColumn && comparison.Lexpr?.ParamRef is ParamRef leftParameter)
            {
                column = rightColumn;
                comparedParameter = leftParameter;
            }
            else
            {
                return false;
            }
            
            if (nullParameter.Number != comparedParameter.Number)
                return false;

            if (!AstColumnReferenceReader.TryRead(column, out columnName, out qualifier))            
                return false;           

            parameterNumber = nullParameter.Number;
            return true;
        }
    }
}