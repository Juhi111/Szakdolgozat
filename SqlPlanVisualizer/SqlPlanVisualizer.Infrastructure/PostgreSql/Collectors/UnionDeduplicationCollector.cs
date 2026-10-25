using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class UnionDeduplicationCollector : CollectorBase<SelectStmt>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(SelectStmt select, AstVisitContext context, SqlStatementSummary summary)
        {
            if (select.Op != SetOperation.SetopUnion || select.All)
                return;

            summary.UnionDeduplications.Add(new UnionDeduplicationOccurrence
            {
                Location = pathFormatter.Format(context),
                SourceOffset = FindLeftTargetOffset(select)
            });
        }

        private static int? FindLeftTargetOffset(SelectStmt select)
        {
            var current = select;
            
            while (current.Larg != null)
                current = current.Larg;

            foreach (var target in current.TargetList)
            {
                var result = target.ResTarget;

                if (result != null && result.Location >= 0)
                    return result.Location;
            }

            return null;
        }
    }
}