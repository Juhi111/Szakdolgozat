using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class RandomOrderingCollector : CollectorBase<SelectStmt>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(SelectStmt select, AstVisitContext context, SqlStatementSummary summary)
        {
            foreach (var sortItem in select.SortClause)
            {
                var function = sortItem.SortBy?.Node?.FuncCall;

                if (function is null || !IsRandomCall(function))
                    continue;

                summary.RandomOrderings.Add(new RandomOrderingOccurrence
                {
                    Location = pathFormatter.Format(context),
                    SourceOffset = function.Location >= 0 ? function.Location : null
                });
            }
        }

        private static bool IsRandomCall(FuncCall function)
        {
            bool supportedName =
                (function.Funcname.Count == 1 && function.Funcname[0].String?.Sval == "random") ||
                (function.Funcname.Count == 2 &&
                 function.Funcname[0].String?.Sval == "pg_catalog" &&
                 function.Funcname[1].String?.Sval == "random");

            return supportedName &&
                   function.Args.Count == 0 &&
                   !function.AggStar &&
                   !function.AggDistinct &&
                   !function.AggWithinGroup &&
                   function.AggOrder.Count == 0 &&
                   function.AggFilter is null &&
                   function.Over is null;
        }
    }
}