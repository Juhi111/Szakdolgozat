using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class DistinctCollector : CollectorBase<SelectStmt>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(SelectStmt selectStatement, AstVisitContext context, SqlStatementSummary summary)
        {
            if (selectStatement.DistinctClause.Count == 0)
                return;

            var firstTarget = selectStatement.TargetList
                .FirstOrDefault()?.ResTarget;

            int? sourceOffset =
                firstTarget != null && firstTarget.Location >= 0
                    ? firstTarget.Location
                    : null;

            summary.DistinctOccurrences.Add(new DistinctOccurrence
            {
                Location = pathFormatter.Format(context),
                IsDistinctOn = selectStatement.DistinctClause[0].NodeCase != Node.NodeOneofCase.None,
                HasJoin = selectStatement.FromClause.Any(fromItem => fromItem.JoinExpr != null),
                SourceOffset = sourceOffset
            });
        }
    }
}