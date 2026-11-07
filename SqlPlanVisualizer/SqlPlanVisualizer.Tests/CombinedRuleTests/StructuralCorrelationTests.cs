using Xunit;

namespace SqlPlanVisualizer.Tests.CombinedRuleTests;

public class StructuralCorrelationTests
{
    [Theory]
    [InlineData("SELECT DISTINCT customer_id FROM customers", "DISTINCT_WITH_SPILL")]
    [InlineData("SELECT customer_id FROM customers UNION SELECT customer_id FROM orders", "UNION_WITH_SPILL")]
    [InlineData("SELECT customer_id, count(*) FROM orders GROUP BY customer_id", "GROUP_BY_WITH_SPILL")]
    public void Analyze_LinksTopLevelHashReductionOnly(string sql, string ruleId)
    {
        const string json = """
            [{"Plan":{"Node Type":"Aggregate","Strategy":"Hashed","Disk Usage":1024,
              "Plans":[{"Node Type":"Seq Scan","Relation Name":"customers"}]}}]
            """;
        var result = CombinedTestData.Analyze(sql, json);
        Assert.Equal(ruleId, Assert.Single(result.CombinedFindings).RuleId);
        Assert.Empty(CombinedTestData.Analyze("SELECT customer_id FROM customers", json).CombinedFindings);
        Assert.Empty(CombinedTestData.Analyze(sql, json.Replace("1024", "0")).CombinedFindings);
    }

    [Fact]
    public void Analyze_DistinctSortIsNotConfusedWithFinalOrderingOrUnionAll()
    {
        const string json = """
            [{"Plan":{"Node Type":"Unique","Plans":[{"Node Type":"Sort","Sort Space Type":"Disk",
              "Plans":[{"Node Type":"Seq Scan","Relation Name":"customers"}]}]}}]
            """;
        Assert.Equal("DISTINCT_WITH_SPILL", Assert.Single(CombinedTestData.Analyze(
            "SELECT DISTINCT customer_id FROM customers", json).CombinedFindings).RuleId);
        Assert.Empty(CombinedTestData.Analyze(
            "SELECT customer_id FROM customers UNION ALL SELECT customer_id FROM orders", json).CombinedFindings);
    }

    [Fact]
    public void Analyze_RandomOrderingRequiresMatchingSortKey()
    {
        const string sql = "SELECT customer_id FROM customers ORDER BY random()";
        const string json = """
            [{"Plan":{"Node Type":"Sort","Sort Space Type":"Disk","Sort Key":["random()"],
              "Plans":[{"Node Type":"Seq Scan","Relation Name":"customers"}]}}]
            """;
        Assert.Contains(CombinedTestData.Analyze(sql, json).CombinedFindings, f => f.RuleId == "RANDOM_ORDERING_WITH_SPILL");
        Assert.DoesNotContain(CombinedTestData.Analyze(sql, json.Replace("random()", "customer_id")).CombinedFindings,
            f => f.RuleId == "RANDOM_ORDERING_WITH_SPILL");
    }
    [Fact]
    public void Analyze_LargeOffsetUsesConfiguredSqlFindingAndExactTopLevelLocation()
    {
        const string sql = "SELECT customer_id FROM customers ORDER BY customer_id LIMIT 100 OFFSET 100000";
        const string json = """
            [{"Plan":{"Node Type":"Limit","Actual Rows":100,"Actual Loops":1,"Plans":[
              {"Node Type":"Sort","Parent Relationship":"Outer","Actual Rows":100100,"Actual Loops":1,"Plans":[
                {"Node Type":"Seq Scan","Parent Relationship":"Outer","Relation Name":"customers",
                 "Actual Rows":250000,"Actual Loops":1}]}]}}]
            """;
        Assert.Contains(CombinedTestData.Analyze(sql, json).CombinedFindings, f => f.RuleId == "LARGE_OFFSET_WITH_LARGE_SORT");
        Assert.DoesNotContain(CombinedTestData.Analyze(sql.Replace("100000", "10"), json).CombinedFindings,
            f => f.RuleId == "LARGE_OFFSET_WITH_LARGE_SORT");
    }

    [Fact]
    public void Analyze_HavingRequiresSameGroupPredicateOnAggregate()
    {
        const string sql = "SELECT c.customer_id, count(*) FROM customers c GROUP BY c.customer_id HAVING c.customer_id > 100";
        const string json = """
            [{"Plan":{"Node Type":"Aggregate","Strategy":"Hashed","Filter":"customer_id > 100",
              "Actual Rows":1,"Actual Loops":1,"Rows Removed by Filter":49999,"Plans":[
              {"Node Type":"Seq Scan","Relation Name":"customers","Alias":"c"}]}}]
            """;
        Assert.Equal("HAVING_FILTER_WITH_HIGH_REMOVAL", Assert.Single(CombinedTestData.Analyze(sql, json).CombinedFindings).RuleId);
        Assert.Empty(CombinedTestData.Analyze(sql, json.Replace("customer_id > 100", "count(*) > 100")).CombinedFindings);
    }
}
