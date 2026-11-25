using PgSqlParser;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class NotInExistsRewriteRule(IEnumerable<DatabaseTableMetadata>? tables) : RelationalRewriteRule(tables)
{
    protected override string RuleId => "NOT_IN_EXISTS_REWRITE";
    protected override string Title => "Hiányzó kapcsolat keresése NOT EXISTS segítségével";
    protected override string Explanation => "A változat azt vizsgálja, hiányzik-e a kapcsolódó sor. Mindkét összehasonlított oszlop tiltja a NULL értéket, így az egyszerű NOT IN feltétel átalakítható.";
    protected override bool Rewrite(SelectStmt s)
    {
        if (s.DistinctClause.Count != 0 || s.FromClause[0].RangeVar is not { } outer || Table(outer) is not { } ot || !Projection(s, outer, ot) ||
            s.WhereClause?.BoolExpr is not { Boolop: BoolExprType.NotExpr } not || not.Args.Count != 1 ||
            not.Args[0].SubLink is not { SubLinkType: SubLinkType.AnySublink } link || link.OperName.Count != 0 ||
            link.Subselect?.SelectStmt is not { } inner || !Simple(inner) || inner.WhereClause is not null || inner.DistinctClause.Count != 0 ||
            inner.TargetList.Count != 1 || inner.FromClause[0].RangeVar is not { } relation || Table(relation) is not { } it ||
            outer.Alias.Aliasname == relation.Alias.Aliasname) return false;
        var left = Column(link.Testexpr, outer, ot); var rightNode = inner.TargetList[0].ResTarget?.Val;
        var right = Column(rightNode, relation, it);
        if (!SameEqualityType(left, right) || left!.IsNullable != false || right!.IsNullable != false) return false;
        s.WhereClause = Exists(relation, RewriteAst.Binary(rightNode!, "=", link.Testexpr), true);
        return true;
    }
}
