using Google.Protobuf;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class WildcardProjectionCollector : ISqlAstCollector
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

            foreach (var target in selectStatement.TargetList)
            {
                var columnRef = target.ResTarget?.Val?.ColumnRef;
                if (columnRef == null)
                    continue;

                var qualifierParts = new List<string>();

                foreach (var field in columnRef.Fields)
                {
                    if (field.String != null)
                        qualifierParts.Add(field.String.Sval);

                    if (field.AStar != null)
                    {
                        summary.WildcardProjections.Add(new WildcardProjectionOccurrence
                        {
                            Location = pathFormatter.Format(entry.Context),
                            SourceOffset = columnRef.Location >= 0 ? columnRef.Location : null,
                            Qualifier = qualifierParts.Count > 0 ? string.Join(".", qualifierParts) : null
                        });
                        break;
                    }
                }
            }
        }
    }
}
