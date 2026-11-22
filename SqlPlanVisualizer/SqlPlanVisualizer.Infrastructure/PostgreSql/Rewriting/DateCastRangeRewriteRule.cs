using System.Globalization;
using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class DateCastRangeRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "DATE_CAST_RANGE_REWRITE";
    protected override string Title => "Dátumra alakítás helyett napi időintervallum";
    protected override string Explanation => "A date casttal vizsgált napot alsó, zárt és felső, nyílt határral fejeztük ki. Az oszlopon így nincs cast, ami segítheti egy megfelelő index használatát.";
    protected override string Preconditions => "Az oszlop beépített date vagy timestamp WITHOUT time zone legyen. A típust nem kérdeztük le. timestamptz oszlopon ezt a változatot ne használd ellenőrzés nélkül: az időzónát és a napváltást külön kezelni kell. Csak explicit DATE 'YYYY-MM-DD' literált írunk át.";
    protected override Node? Rewrite(Node predicate)
    {
        var e = predicate.AExpr;
        if (RewriteAst.Op(e) != "=") return null;
        Node left = e!.Lexpr, right = e.Rexpr;
        if (left?.TypeCast?.Arg?.ColumnRef is null && right?.TypeCast?.Arg?.ColumnRef is not null) (left, right) = (right, left);
        if (left?.TypeCast is not { } cast || !RewriteAst.Type(cast.TypeName, "date") || !RewriteAst.Column(cast.Arg) ||
            right?.TypeCast is not { } date || !RewriteAst.Type(date.TypeName, "date") || !RewriteAst.Text(date.Arg, out string text) ||
            !DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) || start == DateOnly.MaxValue) return null;
        string end = start.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string column = RewriteAst.ColumnSql(cast.Arg);
        return RewriteAst.Expression($"{column} >= DATE '{text}' AND {column} < DATE '{end}'");
    }
}
