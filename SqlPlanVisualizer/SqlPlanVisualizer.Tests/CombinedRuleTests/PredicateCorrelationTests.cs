using Xunit;

namespace SqlPlanVisualizer.Tests.CombinedRuleTests;

public class PredicateCorrelationTests
{
    [Theory]
    [InlineData("c.email LIKE '%adam%'", "email ~~ '%adam%'::text", "LEADING_WILDCARD_WITH_HIGH_REMOVAL")]
    [InlineData("c.email ILIKE '_adam'", "email ~~* '_adam'::text", "LEADING_WILDCARD_WITH_HIGH_REMOVAL")]
    [InlineData("c.email LIKE 'adam'", "email ~~ 'adam'::text", "LIKE_WITHOUT_WILDCARD_WITH_HIGH_REMOVAL")]
    [InlineData("c.customer_id::text = '1'", "(customer_id)::text = '1'::text", "CAST_FILTER_WITH_HIGH_REMOVAL")]
    [InlineData("c.created_at::date = '2026-01-01'", "(created_at)::date = '2026-01-01'::date", "CAST_FILTER_WITH_HIGH_REMOVAL")]
    [InlineData("c.total_amount / 2 > 10", "(total_amount / 2) > 10", "ARITHMETIC_FILTER_WITH_HIGH_REMOVAL")]
    [InlineData("extract(year FROM c.created_at) = 2026", "EXTRACT(year FROM created_at) = 2026", "YEAR_EXTRACTION_WITH_HIGH_REMOVAL")]
    [InlineData("c.first_name || ' ' || c.last_name = 'Adam Kovacs'", "((first_name || ' '::text) || last_name) = 'Adam Kovacs'::text", "CONCATENATION_WITH_HIGH_REMOVAL")]
    [InlineData("($1 IS NULL OR c.customer_id = $1)", "($1 IS NULL OR customer_id = $1)", "OPTIONAL_PARAMETER_WITH_HIGH_REMOVAL")]
    [InlineData("CASE WHEN $1 = 'email' THEN c.email ELSE c.phone END = $2", "CASE WHEN $1 = 'email'::text THEN email ELSE phone END = $2", "CASE_FILTER_WITH_HIGH_REMOVAL")]
    public void Analyze_MatchesParsedExpressionAndRejectsUnrelatedFilter(string predicate, string filter, string ruleId)
    {
        string sql = "SELECT c.customer_id FROM customers c WHERE " + predicate;
        var result = CombinedTestData.Filter(sql, filter);
        Assert.Contains(result.CombinedFindings, f => f.RuleId == ruleId);
        Assert.Empty(CombinedTestData.Filter(sql, "customer_id = 5").CombinedFindings);
    }

    [Theory]
    [InlineData("SELECT 1 FROM customers c WHERE EXISTS (SELECT 1 FROM orders o WHERE lower(o.email) = 'x')")]
    [InlineData("WITH c AS (SELECT * FROM customers) SELECT 1 FROM c WHERE lower(c.email) = 'x'")]
    [InlineData("SELECT 1 FROM customers c JOIN orders o ON c.customer_id = o.customer_id WHERE lower(c.email) = 'x'")]
    public void Analyze_DoesNotInventQueryScopeMatches(string sql)
    {
        Assert.Empty(CombinedTestData.Filter(sql, "lower(email) = 'x'::text").CombinedFindings);
    }

    [Fact]
    public void Analyze_PreservesPatternAndParameterIdentityAndHandlesUnparseableFilter()
    {
        const string like = "SELECT 1 FROM customers c WHERE c.email LIKE '%adam%'";
        Assert.Empty(CombinedTestData.Filter(like, "email ~~ '%eva%'::text").CombinedFindings);
        Assert.Empty(CombinedTestData.Filter(like, "email ~~* '%adam%'::text").CombinedFindings);
        Assert.Empty(CombinedTestData.Filter(like, "(SubPlan 1)").CombinedFindings);
        const string optional = "SELECT 1 FROM customers c WHERE ($1 IS NULL OR c.customer_id = $1)";
        Assert.Empty(CombinedTestData.Filter(optional, "($2 IS NULL OR customer_id = $2)").CombinedFindings);
    }
}
