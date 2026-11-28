using PgSqlParser;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class ComplexSqlRewriteTests
{
    internal static string[] Examples() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "complex-rewrites.sql"))
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static SqlRewriteService Engine() => new(SqlRewriteRuleFactory.CreateDefault());

    [Fact]
    public void UploadedFourExamples_ComposeChangesInJoinsAndSubqueries()
    {
        var examples = Examples();
        Assert.Equal(4, examples.Length);
        string[][] expected =
        [
            ["customer_id > 90", "upper(c.city) = 'BUDAPEST'", "marketing_opt_in IS TRUE", "2027-01-01", "2023-01-01"],
            ["o.total_amount > 400", "2023-01-01", "random()", "ORDER BY random_order"],
            ["o.total_amount > 150", "EXISTS", "NOT EXISTS", "upper(c.city) = 'BUDAPEST'"],
            ["o.total_amount > 200", "o.customer_id > 100", "HAVING count(*) > 1", "2027-01-01", "OFFSET 20000"]
        ];
        for (int i = 0; i < examples.Length; i++)
        {
            var suggestion = Engine().SuggestCombined(examples[i]);
            Assert.NotNull(suggestion);
            Assert.True(suggestion.AppliedSteps.Count >= 2);
            foreach (string text in expected[i]) Assert.Contains(text, suggestion.SuggestedSql);
            Assert.DoesNotContain("::date >=", suggestion.SuggestedSql);
            Assert.DoesNotContain("extract(", suggestion.SuggestedSql);
            Assert.Null(Parser.Parse(suggestion.SuggestedSql).Error);
            Assert.Null(Engine().SuggestCombined(suggestion.SuggestedSql));
            Assert.Equal(examples[i], suggestion.OriginalSql);
        }
    }

    [Fact]
    public void JoinOnAndNestedHaving_AreVisited()
    {
        var on = Assert.Single(new ArithmeticEqualityRewriteRule().Suggest("SELECT a.id FROM lab.a a LEFT JOIN lab.b b ON a.id=b.id AND b.x+1>10"));
        Assert.Contains("b.x > 9", on.SuggestedSql);
        Assert.Contains("LEFT JOIN", on.SuggestedSql);
        var having = Assert.Single(new HavingGroupKeyRewriteRule().Suggest("SELECT * FROM (SELECT x,count(*) FROM lab.t GROUP BY x HAVING x>10) q"));
        Assert.Contains("WHERE x > 10", having.SuggestedSql);
        Assert.DoesNotContain("HAVING", having.SuggestedSql);
    }

    [Fact]
    public void NestedDistinct_UsesOnlyItsOwnTableKey()
    {
        var engine = new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault([RelationalRewriteTests.Table("customers")]));
        var suggestion = engine.SuggestCombined("SELECT * FROM (SELECT DISTINCT c.* FROM lab.customers c WHERE c.id>10) q")!;
        Assert.DoesNotContain("DISTINCT", suggestion.SuggestedSql);
        Assert.True(suggestion.RequiresRelationalMetadata);
    }

    [Theory]
    [InlineData("SELECT a.id FROM lab.a a JOIN lab.b b ON a.id=b.id WHERE b.x+1>10", "b", "x")]
    [InlineData("SELECT a.id FROM lab.a a WHERE EXISTS (SELECT 1 FROM lab.b a WHERE a.x+1>10)", "b", "x")]
    [InlineData("SELECT a.id FROM lab.a a WHERE EXISTS (SELECT 1 FROM lab.b b WHERE b.a_id=a.id AND b.x+1>10)", "b", "x")]
    public void AffectedColumn_IsResolvedInItsOwnScope(string sql, string table, string column)
    {
        var suggestion = Assert.Single(new ArithmeticEqualityRewriteRule().Suggest(sql));
        Assert.Equal(new DatabaseColumnReference("lab", table, column), suggestion.MetadataRequirement!.Column);
    }

    [Theory]
    [InlineData("SELECT a.id FROM lab.a a JOIN lab.b b ON a.id=b.id WHERE x+1>10")]
    [InlineData("SELECT a.id FROM lab.a a WHERE EXISTS (SELECT 1 FROM lab.b b WHERE a.x+1>10)")]
    [InlineData("WITH b AS (SELECT * FROM lab.a) SELECT * FROM b WHERE x+1>10")]
    public void AmbiguousCorrelatedAndCteReferences_AreNotGuessed(string sql) => Assert.Empty(new ArithmeticEqualityRewriteRule().Suggest(sql));

    [Theory]
    [InlineData("upper(c.city) LIKE 'BUDAPEST'", "upper(c.city) = 'BUDAPEST'")]
    [InlineData("lower(c.city) NOT LIKE 'a'", "lower(c.city) <> 'a'")]
    public void BuiltinTextFunction_LiteralLikeKeepsFunction(string predicate, string expected) =>
        Assert.Contains(expected, Assert.Single(new LikeLiteralRewriteRule().Suggest($"SELECT * FROM lab.c c WHERE {predicate}")).SuggestedSql);

    [Theory]
    [InlineData("app.upper(c.city) LIKE 'BUDAPEST'")]
    [InlineData("upper(c.city) LIKE '%BUDAPEST'")]
    [InlineData("upper(c.city) ILIKE 'BUDAPEST'")]
    public void OtherTextSemantics_ArePreserved(string predicate) => Assert.Empty(new LikeLiteralRewriteRule().Suggest($"SELECT * FROM lab.c c WHERE {predicate}"));

    [Theory]
    [InlineData("CASE WHEN c.flag THEN 1 ELSE 0 END = 1")]
    [InlineData("1 = CASE WHEN c.flag THEN 1 ELSE 0 END")]
    public void BooleanCase_PreservesNullAsFalse(string predicate)
    {
        var suggestion = Assert.Single(new BooleanCaseRewriteRule().Suggest($"SELECT * FROM lab.c c WHERE {predicate}"));
        Assert.Contains("c.flag IS TRUE", suggestion.SuggestedSql);
        Assert.Equal(RewriteTypeRequirement.Boolean, suggestion.MetadataRequirement!.Type);
    }

    [Theory]
    [InlineData("CASE WHEN flag THEN 1 END = 1")]
    [InlineData("CASE WHEN flag THEN 1 ELSE 2 END = 1")]
    [InlineData("CASE flag WHEN TRUE THEN 1 ELSE 0 END = 1")]
    public void OtherCaseShapes_ArePreserved(string predicate) => Assert.Empty(new BooleanCaseRewriteRule().Suggest($"SELECT * FROM lab.c WHERE {predicate}"));

    [Theory]
    [InlineData("=", ">=", "2024-01-01")]
    [InlineData(">", ">=", "2025-01-01")]
    [InlineData(">=", ">=", "2024-01-01")]
    [InlineData("<", "<", "2024-01-01")]
    [InlineData("<=", "<", "2025-01-01")]
    public void YearOperators_UseCorrectBoundary(string op, string expectedOp, string date)
    {
        var suggestion = Assert.Single(new YearRangeRewriteRule().Suggest($"SELECT * FROM t WHERE EXTRACT(YEAR FROM d) {op} 2024"));
        Assert.Contains($"d {expectedOp} '{date}'::date", suggestion.SuggestedSql);
    }

    [Theory]
    [InlineData("bool", RewriteTypeRequirement.Boolean, true)]
    [InlineData("int4", RewriteTypeRequirement.Boolean, false)]
    [InlineData("numeric", RewriteTypeRequirement.IntegerOrNumeric, true)]
    [InlineData("float8", RewriteTypeRequirement.IntegerOrNumeric, false)]
    [InlineData("timestamptz", RewriteTypeRequirement.DateTimestampOrTimestampTz, true)]
    [InlineData("interval", RewriteTypeRequirement.DateTimestampOrTimestampTz, false)]
    public void NewRequirements_CheckActualType(string type, RewriteTypeRequirement requirement, bool passes) =>
        Assert.Equal(passes, RewriteMetadataEvaluator.Evaluate(requirement, new("lab", "t", "x", "pg_catalog", type, type, false, true)).Status == MetadataCheckStatus.Passed);
}
