using Google.Protobuf;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class DistinctCollector : ISqlAstCollector
    {
        private readonly AstPathFormatter pathFormatter = new();
        
        public void Collect(AstVisitEntry entry, SqlStatementSummary summary)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            if (summary is null)
                throw new ArgumentNullException(nameof(summary));

            if (entry.Node is not SelectStmt selectStatement)
                return;

            if (selectStatement.DistinctClause.Count == 0)
                return;

            var firstTarget = selectStatement.TargetList.FirstOrDefault()?.ResTarget;

            int? sourceOffset = firstTarget != null && firstTarget.Location >= 0 ? firstTarget.Location : null;

            summary.DistinctOccurrences.Add(new DistinctOccurrence
            {
                Location = pathFormatter.Format(entry.Context),

                IsDistinctOn = selectStatement.DistinctClause[0].NodeCase != PgSqlParser.Node.NodeOneofCase.None,

                HasJoin = selectStatement.FromClause.Any(fromItem => fromItem.JoinExpr != null),

                SourceOffset = sourceOffset
            });
        }
    }
}
