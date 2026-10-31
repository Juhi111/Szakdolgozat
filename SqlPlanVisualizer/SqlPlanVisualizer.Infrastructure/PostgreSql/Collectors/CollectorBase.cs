using Google.Protobuf;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public abstract class CollectorBase<TNode> : ISqlAstCollector where TNode : class, IMessage
    {
        public void Collect(AstVisitEntry entry, SqlStatementSummary summary)
        {

            ArgumentNullException.ThrowIfNull(nameof(entry));
            ArgumentNullException.ThrowIfNull(nameof(summary));         

            if (entry.Node is not TNode node)
                return;

            CollectNode(node, entry.Context, summary);
        }

        protected abstract void CollectNode(TNode node, AstVisitContext context, SqlStatementSummary summary);
    }
}