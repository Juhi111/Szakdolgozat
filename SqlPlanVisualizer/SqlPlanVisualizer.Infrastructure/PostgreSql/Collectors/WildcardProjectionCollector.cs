using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class WildcardProjectionCollector : CollectorBase<SelectStmt>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(SelectStmt selectStatement, AstVisitContext context, SqlStatementSummary summary)
        {
            if (IsDirectSimpleExistsQuery(selectStatement, context))
                return;

            foreach (var target in selectStatement.TargetList)
            {
                var columnRef = target.ResTarget?.Val?.ColumnRef;

                if (columnRef is null)
                    continue;

                var qualifierParts = new List<string>();

                foreach (var field in columnRef.Fields)
                {
                    if (field.String != null)
                        qualifierParts.Add(field.String.Sval);

                    if (field.AStar is null)
                        continue;

                    summary.WildcardProjections.Add(new WildcardProjectionOccurrence
                    {
                        Location = pathFormatter.Format(context),
                        Qualifier = qualifierParts.Count > 0 ? string.Join(".", qualifierParts) : null,
                        SourceOffset = columnRef.Location >= 0 ? columnRef.Location : null
                    });

                    break;
                }
            }
        }

        private static bool IsDirectSimpleExistsQuery(SelectStmt selectStatement, AstVisitContext context)
        {
            return selectStatement.Op == SetOperation.SetopNone
                && context.Parent?.Parent?.Node is SubLink subLink
                && subLink.SubLinkType == SubLinkType.ExistsSublink
                && ReferenceEquals(subLink.Subselect?.SelectStmt, selectStatement);
        }
    }
}