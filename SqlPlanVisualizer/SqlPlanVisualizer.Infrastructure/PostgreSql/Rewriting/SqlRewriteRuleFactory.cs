using SqlPlanVisualizer.Core.Interfaces;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>Az SQL-átírások egyetlen központi regisztrációs helye.</summary>
public static class SqlRewriteRuleFactory
{
    public static IReadOnlyList<ISqlRewriteRule> CreateDefault(IEnumerable<SqlPlanVisualizer.Core.Models.Database.DatabaseTableMetadata>? tables = null) => new ISqlRewriteRule[]
    {
        new ArithmeticEqualityRewriteRule(),
        new CountExistenceRewriteRule(),
        new LikeLiteralRewriteRule(),
        new BooleanCaseRewriteRule(),
        new DateCastRangeRewriteRule(),
        new YearRangeRewriteRule(),
        new HavingGroupKeyRewriteRule(),
        new OrToInRewriteRule(),
        new SingletonInRewriteRule(),
        new DuplicateInRewriteRule(),
        new NotInExistsRewriteRule(tables),
        new LeftJoinAntiRewriteRule(tables),
        new RedundantDistinctRewriteRule(tables),
        new DistinctJoinExistsRewriteRule(tables)
    };
}
