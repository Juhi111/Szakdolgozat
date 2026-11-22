using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

internal static class ListPredicatePattern
{
    public static bool InList(Node node, out A_Expr expression, out List<Node> values)
    {
        expression = node.AExpr!;
        values = expression?.Rexpr?.List?.Items.ToList() ?? new();
        return expression?.Kind == A_Expr_Kind.AexprIn && expression.Name.Count == 1 &&
            expression.Name[0].String?.Sval is "=" or "<>" && RewriteAst.Column(expression.Lexpr) && RewriteAst.Homogeneous(values);
    }
    public static bool OrEquals(Node node, out Node column, out List<Node> values)
    {
        column = null!;
        values = new();
        if (node.BoolExpr is not { Boolop: BoolExprType.OrExpr } boolean || boolean.Args.Count < 2) return false;
        foreach (var arg in boolean.Args)
        {
            var e = arg.AExpr;
            if (RewriteAst.Op(e) != "=" || !RewriteAst.Column(e!.Lexpr) || !RewriteAst.Literal(e.Rexpr)) return false;
            if (column is not null && !RewriteAst.SameColumn(column, e.Lexpr)) return false;
            column = e.Lexpr;
            values.Add(e.Rexpr);
        }
        return RewriteAst.Homogeneous(values);
    }
    public static int? Offset(Node node) => node.AExpr is { Location: >= 0 } e ? e.Location :
        node.BoolExpr is { Location: >= 0 } b ? b.Location : null;
}
