using System.Globalization;
using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class DateCastRangeRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "DATE_CAST_RANGE_REWRITE";
    protected override string Title => "Dátumra alakítás helyett napi időintervallum";
    protected override string Explanation => "A dátumra alakított oszlop összehasonlítását közvetlen időhatárokkal fejeztük ki. Az oszlopon így nincs cast, ami segítheti egy megfelelő index használatát.";
    protected override string Preconditions => "Beépített date, timestamp vagy timestamptz oszlopra alkalmazható. A DATE határokat a PostgreSQL az oszloptípusnak megfelelően hasonlítja össze; timestamptz esetén az aktuális munkamenet időzónája számít. Az eredeti és a javasolt SQL-t azonos időzónával hasonlítsd össze. Csak explicit DATE 'YYYY-MM-DD' literált támogatunk; történeti időzóna-ugrásoknál a napkezdetet külön ellenőrizd.";
    internal static string Reverse(string op) => op switch { "<" => ">", "<=" => ">=", ">" => "<", ">=" => "<=", _ => op };

    protected override Node? Rewrite(Node predicate)
    {
        var e = predicate.AExpr;
        string op = RewriteAst.Op(e);
        if (op is not ("=" or "<" or "<=" or ">" or ">=")) return null;
        Node left = e!.Lexpr, right = e.Rexpr;
        if (left?.TypeCast?.Arg?.ColumnRef is null && right?.TypeCast?.Arg?.ColumnRef is not null) { (left, right) = (right, left); op = Reverse(op); }
        if (left?.TypeCast is not { } cast || !RewriteAst.Type(cast.TypeName, "date") || !RewriteAst.Column(cast.Arg) ||
            right?.TypeCast is not { } date || !RewriteAst.Type(date.TypeName, "date") || !RewriteAst.Text(date.Arg, out string text) ||
            !DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) || start == DateOnly.MaxValue) return null;
        string end = start.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string column = RewriteAst.ColumnSql(cast.Arg);
        return RewriteAst.Expression(op switch
        {
            "=" => $"{column} >= DATE '{text}' AND {column} < DATE '{end}'",
            ">=" => $"{column} >= DATE '{text}'",
            ">" => $"{column} >= DATE '{end}'",
            "<" => $"{column} < DATE '{text}'",
            _ => $"{column} < DATE '{end}'"
        });
    }
}
