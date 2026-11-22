using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class OrToInRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "OR_TO_IN_REWRITE";
    protected override string Title => "Azonos oszlop egyenlőségeinek összevonása IN-listába";
    protected override string Explanation => "Az OR minden ága ugyanazt az oszlopot hasonlította homogén literálokhoz. Az IN-lista tömörebben fejezi ki ezt; a PostgreSQL terve ettől még változatlan lehet.";
    protected override string Preconditions => "Csak egyszerű oszlop és azonos fajtájú egész vagy szöveg literálok. NULL, függvény, cast, eltérő oszlop, vegyes literáltípus és összetett OR-ág esetén nincs átírás.";
    protected override Node? Rewrite(Node predicate)
    {
        if (!ListPredicatePattern.OrEquals(predicate, out var column, out var values)) return null;
        var node = RewriteAst.Expression("x IN (1, 2)");
        node.AExpr.Lexpr = column.Clone();
        node.AExpr.Rexpr.List.Items.Clear();
        node.AExpr.Rexpr.List.Items.Add(values.Select(n => n.Clone()));
        return node;
    }
}
