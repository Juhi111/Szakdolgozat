using PgSqlParser;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class RedundantDistinctRewriteRule(IEnumerable<DatabaseTableMetadata>? tables) : RelationalRewriteRule(tables)
{
    protected override string RuleId => "REDUNDANT_DISTINCT_REWRITE";
    protected override string Title => "A kulcs már biztosítja az egyediséget";
    protected override string Explanation => "A kiválasztott oszlopok tartalmazzák a tábla teljes, nem halasztható elsődleges kulcsát. Ebben az egyszerű lekérdezésben a DISTINCT elhagyható.";
    protected override bool Rewrite(SelectStmt s)
    {
        if (!PlainDistinct(s) || s.WhereClause is not null || s.FromClause[0].RangeVar is not { } relation || Table(relation) is not { } table || !UniqueProjection(s, relation, table)) return false;
        s.DistinctClause.Clear(); return true;
    }
}
