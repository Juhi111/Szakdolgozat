using System.Globalization;
using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class YearRangeRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "YEAR_RANGE_REWRITE";
    protected override string Title => "Évkinyerés helyett éves időintervallum";
    protected override string Explanation => "Az EXTRACT(YEAR ...) vagy date_part('year', ...) egyenlőségi szűrését az év kezdetétől a következő év kezdetéig tartó intervallumra alakítottuk.";
    protected override string Preconditions => "Az oszlop date vagy timestamp WITHOUT time zone legyen; interval és timestamptz esetén ez az átírás nincs igazolva. A típust nem ellenőriztük. Az év 1 és 9998 közötti egész literál; a BC-évek, speciális értékek és egyéni függvények nem támogatottak.";
    protected override Node? Rewrite(Node predicate)
    {
        var e = predicate.AExpr;
        if (RewriteAst.Op(e) != "=") return null;
        Node left = e!.Lexpr, right = e.Rexpr;
        if (left?.FuncCall is null && right?.FuncCall is not null) (left, right) = (right, left);
        if (left?.FuncCall is not { } f || RewriteAst.Name(f) is not ("extract" or "date_part") || f.Args.Count != 2 ||
            f.Over is not null || f.AggFilter is not null || f.AggDistinct || f.AggOrder.Count != 0 ||
            !RewriteAst.Text(f.Args[0], out var part) || !part.Equals("year", StringComparison.OrdinalIgnoreCase) ||
            !RewriteAst.Column(f.Args[1]) || !RewriteAst.Integer(right, out int year) || year < 1 || year > 9998) return null;
        string column = RewriteAst.ColumnSql(f.Args[1]);
        string start = year.ToString("D4", CultureInfo.InvariantCulture), end = (year + 1).ToString("D4", CultureInfo.InvariantCulture);
        return RewriteAst.Expression($"{column} >= DATE '{start}-01-01' AND {column} < DATE '{end}-01-01'");
    }
}
