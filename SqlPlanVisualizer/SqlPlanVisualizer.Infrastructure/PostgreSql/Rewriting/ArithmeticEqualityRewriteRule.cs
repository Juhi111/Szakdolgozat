using System.Globalization;
using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>Összeadás/kivonás áthelyezése egész literálos összehasonlításban.</summary>
public sealed class ArithmeticEqualityRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "ARITHMETIC_EQUALITY_REWRITE";
    protected override string Title => "Az oszlopon végzett összeadás vagy kivonás áthelyezése";
    protected override string Explanation => "A konstanssal végzett összeadást vagy kivonást áthelyeztük a másik oldalra. Az oszlop önmagában szerepelhet a szűrésben. A fordított összehasonlítás irányát is megőrizzük.";
    protected override string Preconditions => "Az oszlop beépített egész vagy pontos numeric típusú legyen. Az eredeti művelet nem okozhat túlcsordulást vagy más hibát; " +
        "az átírás különben a hibaviselkedést is megváltoztathatja. Egyéni típusokhoz és lebegőpontos adatokhoz nem igazolt. " +
        "A konstansok és az új határ int32 tartományban maradnak. Szorzást/osztást nem írunk át.";
    protected override Node? Rewrite(Node predicate)
    {
        var e = predicate.AExpr;
        string op = RewriteAst.Op(e);
        if (op is not ("=" or "<>" or "<" or "<=" or ">" or ">=")) return null;
        Node left = e!.Lexpr, right = e.Rexpr;
        if (left?.AExpr is null && right?.AExpr is not null)
        {
            (left, right) = (right, left);
            op = op switch { ">" => "<", "<" => ">", ">=" => "<=", "<=" => ">=", _ => op };
        }
        if (left?.AExpr is not { } arithmetic || RewriteAst.Op(arithmetic) is not ("+" or "-") ||
            !RewriteAst.Integer(right, out int target)) return null;
        Node column = arithmetic.Lexpr, constant = arithmetic.Rexpr;
        if (RewriteAst.Op(arithmetic) == "+" && !RewriteAst.Column(column) && RewriteAst.Column(constant)) (column, constant) = (constant, column);
        if (!RewriteAst.Column(column) || !RewriteAst.Integer(constant, out int delta)) return null;
        long adjusted = RewriteAst.Op(arithmetic) == "+" ? (long)target - delta : (long)target + delta;
        if (adjusted < int.MinValue || adjusted > int.MaxValue) return null;
        var number = RewriteAst.Expression("0 = " + adjusted.ToString(CultureInfo.InvariantCulture)).AExpr.Rexpr;
        return RewriteAst.Binary(column, op, number);
    }
}
