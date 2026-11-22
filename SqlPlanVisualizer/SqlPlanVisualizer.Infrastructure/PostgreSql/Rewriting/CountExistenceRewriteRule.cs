using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class CountExistenceRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "COUNT_EXISTENCE_REWRITE";
    protected override string Title => "Darabszám helyett létezésvizsgálat";
    protected override string Explanation => "A COUNT(*) összehasonlítása csak a megfelelő sor létezését vizsgálta. EXISTS vagy NOT EXISTS alakot készítettünk, a korrelált szűrés megtartásával.";
    protected override string Preconditions => "Egyszerű, egy táblás COUNT(*) allekérdezés; nincs GROUP BY, HAVING, LIMIT, OFFSET vagy függvényes szűrés. A szűrés kiértékelése ne okozzon hibát vagy mellékhatást: az EXISTS korábban leállhat.";
    protected override Node? Rewrite(Node predicate)
    {
        if (predicate.AExpr is not { } expression || !CountExistencePattern.TryRead(expression, out var select, out bool negate)) return null;
        var query = select.Clone();
        query.TargetList.Clear();
        query.TargetList.Add(RewriteAst.Parse("SELECT 1").Stmts[0].Stmt.SelectStmt.TargetList[0].Clone());
        var exists = new Node { SubLink = new SubLink { SubLinkType = SubLinkType.ExistsSublink, Subselect = new Node { SelectStmt = query } } };
        if (!negate) return exists;
        var not = new BoolExpr { Boolop = BoolExprType.NotExpr };
        not.Args.Add(exists);
        return new Node { BoolExpr = not };
    }
}
