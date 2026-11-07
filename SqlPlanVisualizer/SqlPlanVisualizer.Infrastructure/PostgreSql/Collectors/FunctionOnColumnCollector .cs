using PgSqlParser;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class FunctionOnColumnCollector : CollectorBase<FuncCall>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(
            FuncCall function,
            AstVisitContext context,
            SqlStatementSummary summary)
        {            
            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)
                return;
            
            if (!TryReadFunctionName(function, out string functionName))
                return;
            
            if (function.Args.Count != 1)
                return;
            
            var column = function.Args[0].ColumnRef;

            if (column is null)
                return;

            if (!AstColumnReferenceReader.TryRead(column, out string columnName, out string? qualifier))            
                return;


            var occurrence = new FunctionOnColumnOccurrence
            {
                FunctionName = functionName,
                ColumnName = columnName,
                Qualifier = qualifier,
                Location = pathFormatter.Format(context),
                SourceOffset = function.Location >= 0 ? function.Location : null
            };

            summary.FunctionOnColumnOccurrences.Add(occurrence);

            if (clause == AstClause.Where && IsTopLevelQuery(context))
            {
                summary.TopLevelQuery?.FunctionsInWhere.Add(occurrence);
            }
        }

        /// <summary>
        /// Ellenőrzi, hogy a függvény a felső szintű SELECT lekérdezéshez tartozik-e.
        /// </summary>
        private bool IsTopLevelQuery(AstVisitContext context)
        {
            var selectContext = contextResolver.FindSelectContext(context);

            if (selectContext is null)
                return false;

            var ancestor = selectContext.Parent;

            while (ancestor is not null)
            {
                if (ancestor.NodeType == typeof(SelectStmt))
                    return false;

                ancestor = ancestor.Parent;
            }

            return true;
        }

        private static bool TryReadFunctionName(FuncCall function, out string functionName)
        {
            functionName = string.Empty;

            string? name;

            if (function.Funcname.Count == 1)
            {                
                name = function.Funcname[0].String?.Sval;
            }
            else if (function.Funcname.Count == 2 &&
                     function.Funcname[0].String?.Sval == "pg_catalog")
            {             
                name = function.Funcname[1].String?.Sval;
            }
            else
            {             
                return false;
            }

            if (name != "lower" && name != "upper")
                return false;

            functionName = name;
            return true;
        }        
    }
}