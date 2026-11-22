using PgSqlParser;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurrences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors;

public sealed class PredicateSimplificationCollector : CollectorBase<Node>
{
    private readonly AstContextResolver _resolver = new();
    private readonly AstPathFormatter _formatter = new();
    protected override void CollectNode(Node node, AstVisitContext context, SqlStatementSummary summary)
    {
        if (_resolver.GetClause(context) is not (AstClause.Where or AstClause.JoinOn)) return;
        PredicateSimplificationKind? kind = null;
        if (ListPredicatePattern.OrEquals(node, out _, out _)) kind = PredicateSimplificationKind.OrEqualityChain;
        else if (ListPredicatePattern.InList(node, out _, out var values))
        {
            if (values.Count == 1) kind = PredicateSimplificationKind.SingletonIn;
            else if (values.DistinctBy(RewriteAst.LiteralKey).Count() < values.Count) kind = PredicateSimplificationKind.DuplicateInValues;
        }
        if (kind is null) return;
        summary.PredicateSimplifications.Add(new PredicateSimplificationOccurrence
        {
            Kind = kind.Value, Location = _formatter.Format(context), SourceOffset = ListPredicatePattern.Offset(node)
        });
    }
}
