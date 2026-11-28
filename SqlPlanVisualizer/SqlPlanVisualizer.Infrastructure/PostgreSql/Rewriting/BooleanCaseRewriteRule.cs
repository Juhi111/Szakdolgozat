using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>A logikai oszlopot 1/0 értékké alakító CASE szűrésének egyszerűsítése.</summary>
public sealed class BooleanCaseRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "BOOLEAN_CASE_REWRITE";
    protected override string Title => "Logikai feltétel egyszerűsítése CASE helyett";
    protected override string Explanation => "Az 1/0 értéket előállító CASE helyett közvetlenül vizsgáljuk, hogy a logikai oszlop igaz-e. A NULL érték kezelése megmarad.";
    protected override string Preconditions => "Csak egyetlen logikai oszlopot vizsgáló CASE WHEN oszlop THEN 1 ELSE 0 END = 1 alakra alkalmazzuk.";
    protected override Node? Rewrite(Node predicate)
    {
        if (predicate.AExpr is not { } e || RewriteAst.Op(e) != "=") return null;
        Node left = e.Lexpr, right = e.Rexpr;
        if (right?.CaseExpr is not null) (left, right) = (right, left);
        if (!RewriteAst.Integer(right, out int expected) || expected != 1 || left?.CaseExpr is not { Arg: null } c ||
            c.Args.Count != 1 || c.Args[0].CaseWhen is not { } when || !RewriteAst.Column(when.Expr) ||
            !RewriteAst.Integer(when.Result, out int yes) || yes != 1 || !RewriteAst.Integer(c.Defresult, out int no) || no != 0) return null;
        return RewriteAst.Expression($"{RewriteAst.ColumnSql(when.Expr)} IS TRUE");
    }
}
