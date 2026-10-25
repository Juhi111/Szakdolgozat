using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class NaturalJoinCollector : CollectorBase<JoinExpr>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(JoinExpr join, AstVisitContext context, SqlStatementSummary summary)
        {
            if (!join.IsNatural)
                return;

            int? sourceOffset = GetRelationOffset(join.Rarg) ?? GetRelationOffset(join.Larg);

            summary.NaturalJoins.Add(new NaturalJoinOccurrence
            {
                Location = pathFormatter.Format(context),
                SourceOffset = sourceOffset
            });
        }

        private static int? GetRelationOffset(Node? node)
        {
            var relation = node?.RangeVar;

            return relation != null && relation.Location >= 0
                ? relation.Location
                : null;
        }
    }
}