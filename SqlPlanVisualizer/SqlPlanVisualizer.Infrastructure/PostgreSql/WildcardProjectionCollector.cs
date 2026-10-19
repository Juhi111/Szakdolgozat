using Google.Protobuf;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class WildcardProjectionCollector
    {
        public List<WildcardProjectionOccurrence> Collect(IMessage root, int statementIndex) {

            List<WildcardProjectionOccurrence> occurences = new();

            var walker = new PostgreSqlAstWalker();

            var pathFormatter = new AstPathFormatter();

            walker.Walk(root, statementIndex, entry => {
                if (entry.Node is PgSqlParser.SelectStmt selectStatement) {
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
                                occurences.Add(new WildcardProjectionOccurrence
                                {
                                    Location = pathFormatter.Format(entry.Context),
                                    SourceOffset = columnRef.Location >= 0 ? columnRef.Location : null,
                                    Qualifier = qualifierParts.Count > 0
                                        ? string.Join(".", qualifierParts)
                                        : null
                                });

                                break;
                            }
                        }
                    }
                }
            });
            return occurences;
        }
    }
}
