using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class CountForExistenceCollector : CollectorBase<A_Expr>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
        {
            
            if (expression.Kind != A_Expr_Kind.AexprOp || expression.Name.Count != 1 || expression.Name[0].String?.Sval != ">")
            {
                return;
            }
            
            var constant = expression.Rexpr?.AConst;

            if (constant is null || constant.Isnull || constant.Ival is null || constant.Ival.Ival != 0)
                return;

            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)
                return;

            var subLink = expression.Lexpr?.SubLink;

            if (subLink is null || subLink.SubLinkType != SubLinkType.ExprSublink)
                return;

            var select = subLink.Subselect?.SelectStmt;

            if (select is null || !IsSupportedSelect(select))
                return;

            var function = select.TargetList[0].ResTarget?.Val?.FuncCall;

            if (function is null || !IsPlainCountStar(function))
                return;

            summary.CountForExistenceOccurrences.Add(new CountForExistenceOccurrence
            {
                Location = pathFormatter.Format(context),
                SourceOffset = function.Location >= 0
                    ? function.Location
                    : expression.Location >= 0 ? expression.Location : null
            });
        }

        private static bool IsSupportedSelect(SelectStmt select)
        {
            return select.TargetList.Count == 1 &&
                   select.FromClause.Count > 0 &&
                   select.Op == SetOperation.SetopNone &&
                   select.GroupClause.Count == 0 &&
                   select.HavingClause is null &&
                   select.LimitCount is null &&
                   select.LimitOffset is null &&
                   select.DistinctClause.Count == 0 &&
                   select.WindowClause.Count == 0 &&
                   select.SortClause.Count == 0 &&
                   select.WithClause is null &&
                   select.LockingClause.Count == 0 &&
                   select.IntoClause is null;
        }

        private static bool IsPlainCountStar(FuncCall function)
        {
            bool supportedName =
                (function.Funcname.Count == 1 &&
                 function.Funcname[0].String?.Sval == "count") ||
                (function.Funcname.Count == 2 &&
                 function.Funcname[0].String?.Sval == "pg_catalog" &&
                 function.Funcname[1].String?.Sval == "count");

            if (!supportedName)
                return false;

            return function.AggStar &&
                   function.Args.Count == 0 &&
                   !function.AggDistinct &&
                   !function.AggWithinGroup &&
                   function.AggOrder.Count == 0 &&
                   function.AggFilter is null &&
                   function.Over is null;
        }
    }
}