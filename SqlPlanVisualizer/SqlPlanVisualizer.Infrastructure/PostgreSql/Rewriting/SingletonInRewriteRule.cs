using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class SingletonInRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "SINGLETON_IN_REWRITE";
    protected override string Title => "Egyelemű IN-lista egyszerűsítése";
    protected override string Explanation => "Az egyelemű IN-listát =, a NOT IN-listát <> összehasonlításra cseréltük. Ez olvashatósági egyszerűsítés, nem önmagában teljesítményhiba.";
    protected override string Preconditions => "Egyetlen nem NULL egész vagy szöveg literál és egyszerű oszlop. Allekérdezést, tömböt, paramétert és sorértéket nem írunk át.";
    protected override Node? Rewrite(Node predicate) => ListPredicatePattern.InList(predicate, out var e, out var values) && values.Count == 1
        ? RewriteAst.Binary(e.Lexpr, e.Name[0].String.Sval, values[0]) : null;
}
