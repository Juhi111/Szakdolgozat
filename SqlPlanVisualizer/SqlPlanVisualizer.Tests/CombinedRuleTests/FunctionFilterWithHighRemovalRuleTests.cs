using Xunit;

namespace SqlPlanVisualizer.Tests.CombinedRuleTests;

public class FunctionFilterWithHighRemovalRuleTests
{
    [Fact]
    public void Analyze_ReturnsFindingOnlyForMatchingRelationAndExpression()
    {
        const string sql = "SELECT c.customer_id FROM customers c WHERE lower(c.email) = 'adam@example.com'";
        const string filter = "lower(email) = 'adam@example.com'::text";
        var result = CombinedTestData.Filter(sql, filter);
        Assert.Equal("FUNCTION_FILTER_WITH_HIGH_REMOVAL", Assert.Single(result.CombinedFindings).RuleId);
        Assert.Empty(CombinedTestData.Filter(sql, filter, alias: "other").CombinedFindings);
        Assert.Empty(CombinedTestData.Filter(sql, filter, table: "orders").CombinedFindings);
        Assert.Empty(CombinedTestData.Filter(sql, "email = 'lower(email)'::text").CombinedFindings);
    }
}
