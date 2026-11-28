using System.Globalization;
using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class YearRangeRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "YEAR_RANGE_REWRITE";
    protected override string Title => "Évkinyerés helyett éves időintervallum";
    protected override string Explanation => "Az EXTRACT(YEAR ...) vagy date_part('year', ...) szűrését közvetlen évhatárokkal fejeztük ki.";
    protected override string Preconditions => "Beépített date, timestamp vagy timestamptz oszlop szükséges. Timestamptz esetén a DATE határok és az évkinyerés is a munkamenet időzónáját használják: az összehasonlításnál azonos időzónát használj. Csak 1 és 9998 közötti egész évet támogatunk; történeti időzóna-ugrásoknál a napkezdetet külön ellenőrizd.";
    protected override Node? Rewrite(Node predicate)
    {
        var e = predicate.AExpr;
        string op = RewriteAst.Op(e);
        if (op is not ("=" or "<" or "<=" or ">" or ">=")) return null;
        Node left = e!.Lexpr, right = e.Rexpr;
        if (left?.FuncCall is null && right?.FuncCall is not null) { (left, right) = (right, left); op = DateCastRangeRewriteRule.Reverse(op); }
        if (left?.FuncCall is not { } f || RewriteAst.Name(f) is not ("extract" or "date_part") || f.Args.Count != 2 ||
            f.Over is not null || f.AggFilter is not null || f.AggDistinct || f.AggOrder.Count != 0 ||
            !RewriteAst.Text(f.Args[0], out var part) || !part.Equals("year", StringComparison.OrdinalIgnoreCase) ||
            !RewriteAst.Column(f.Args[1]) || !RewriteAst.Integer(right, out int year) || year < 1 || year > 9998) return null;
        string column = RewriteAst.ColumnSql(f.Args[1]);
        string start = year.ToString("D4", CultureInfo.InvariantCulture), end = (year + 1).ToString("D4", CultureInfo.InvariantCulture);
        return RewriteAst.Expression(op switch
        {
            "=" => $"{column} >= DATE '{start}-01-01' AND {column} < DATE '{end}-01-01'",
            ">=" => $"{column} >= DATE '{start}-01-01'",
            ">" => $"{column} >= DATE '{end}-01-01'",
            "<" => $"{column} < DATE '{start}-01-01'",
            _ => $"{column} < DATE '{end}-01-01'"
        });
    }
}
