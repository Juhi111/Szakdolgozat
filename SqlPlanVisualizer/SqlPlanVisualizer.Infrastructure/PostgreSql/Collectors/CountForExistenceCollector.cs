using PgSqlParser;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurrences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors;

public class CountForExistenceCollector : CollectorBase<A_Expr>
{
    private readonly AstPathFormatter _pathFormatter = new();
    private readonly AstContextResolver _contextResolver = new();
    protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
    {
        if (_contextResolver.GetClause(context) is not (AstClause.Where or AstClause.JoinOn) ||
            !CountExistencePattern.TryRead(expression, out var select, out _, forRewrite: false)) return;
        int location = select.TargetList[0].ResTarget.Val.FuncCall.Location;
        summary.CountForExistenceOccurrences.Add(new CountForExistenceOccurrence
        {
            Location = _pathFormatter.Format(context),
            SourceOffset = location >= 0 ? location : expression.Location >= 0 ? expression.Location : null
        });
    }
}
