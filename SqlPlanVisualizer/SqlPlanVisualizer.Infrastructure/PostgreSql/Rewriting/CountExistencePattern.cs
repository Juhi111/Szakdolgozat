using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>A COUNT(*) létezést vagy nemlétezést vizsgáló összehasonlításainak közös felismerése.</summary>
internal static class CountExistencePattern
{
    public static bool TryRead(A_Expr expression, out SelectStmt select, out bool negate, bool forRewrite = true)
    {
        select = null!;
        negate = false;
        string op = RewriteAst.Op(expression);
        Node? left = expression.Lexpr, right = expression.Rexpr;
        if (left?.SubLink is null && right?.SubLink is not null)
        {
            (left, right) = (right, left);
            op = op switch { ">" => "<", "<" => ">", ">=" => "<=", "<=" => ">=", _ => op };
        }
        if (left?.SubLink is not { SubLinkType: SubLinkType.ExprSublink } link || !RewriteAst.Integer(right, out int value)) return false;
        bool exists = (value == 0 && op is ">" or "<>" or "!=") || (value == 1 && op == ">=");
        negate = (value == 0 && op is "=" or "<=") || (value == 1 && op == "<");
        if (!exists && !negate) return false;
        select = link.Subselect?.SelectStmt!;
        if (select is null || select.Op != SetOperation.SetopNone || select.WithClause is not null || select.IntoClause is not null ||
            select.LockingClause.Count != 0 || select.FromClause.Count == 0 || (forRewrite && !RewriteAst.Supported(select)) ||
            select.TargetList.Count != 1 || select.GroupClause.Count != 0 ||
            select.HavingClause is not null || select.LimitCount is not null || select.LimitOffset is not null ||
            select.DistinctClause.Count != 0 || select.WindowClause.Count != 0 || select.SortClause.Count != 0) return false;
        var function = select.TargetList[0].ResTarget?.Val?.FuncCall;
        if (function is null || RewriteAst.Name(function) != "count" || !function.AggStar || function.Args.Count != 0 ||
            function.AggDistinct || function.AggWithinGroup || function.AggOrder.Count != 0 || function.AggFilter is not null || function.Over is not null) return false;
        // A korai leállás miatt ne írjunk át függvényhívásos vagy további allekérdezést tartalmazó szűrést.
        return !forRewrite || select.WhereClause is null || !RewriteAst.Contains(select.WhereClause, n => n is FuncCall or SubLink or TypeCast);
    }
}
