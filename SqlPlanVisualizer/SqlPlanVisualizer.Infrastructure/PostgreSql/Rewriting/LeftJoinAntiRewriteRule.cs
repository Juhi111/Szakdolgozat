using PgSqlParser;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class LeftJoinAntiRewriteRule(IEnumerable<DatabaseTableMetadata>? tables) : RelationalRewriteRule(tables)
{
    protected override string RuleId => "LEFT_JOIN_ANTI_REWRITE";
    protected override string Title => "Hiányzó kapcsolatok közvetlen vizsgálata";
    protected override string Explanation => "A LEFT JOIN és az üres jobb oldal keresése helyett NOT EXISTS ellenőrzi, hogy nincs kapcsolódó sor. A kimenetben csak a bal tábla oszlopai szerepelnek.";
    protected override bool Rewrite(SelectStmt s)
    {
        if (s.DistinctClause.Count != 0 || !Join(s, JoinType.JoinLeft, out var join, out var left, out var right, out _, out var rt) ||
            s.WhereClause?.NullTest is not { Nulltesttype: NullTestType.IsNull, Argisrow: false } test ||
            Column(test.Arg, right, rt)?.IsNullable != false) return false;
        ReplaceJoin(s, left, right, join.Quals, true);
        return true;
    }
}
