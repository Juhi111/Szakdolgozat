using PgSqlParser;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class DistinctJoinExistsRewriteRule(IEnumerable<DatabaseTableMetadata>? tables) : RelationalRewriteRule(tables)
{
    protected override string RuleId => "DISTINCT_JOIN_EXISTS_REWRITE";
    protected override string Title => "Összekapcsolás helyett a találat létezését vizsgáljuk";
    protected override string Explanation => "Csak a bal tábla adataira van szükség, teljes elsődleges kulccsal. Az EXISTS elkerüli a jobb oldali találatok miatti ismétlődést, ezért a DISTINCT is elhagyható.";
    protected override bool Rewrite(SelectStmt s)
    {
        if (!PlainDistinct(s) || s.WhereClause is not null || !Join(s, JoinType.JoinInner, out var join, out var left, out var right, out var lt, out _) || !UniqueProjection(s, left, lt)) return false;
        ReplaceJoin(s, left, right, join.Quals, false); s.DistinctClause.Clear(); return true;
    }
}
