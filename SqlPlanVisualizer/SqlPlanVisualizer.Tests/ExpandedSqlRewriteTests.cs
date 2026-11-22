using PgSqlParser;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.Rules;
using SqlPlanVisualizer.Core.Models.Occurrences;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class ExpandedSqlRewriteTests
{
    private static readonly SqlRewriteService Service = new(SqlRewriteRuleFactory.CreateDefault());

    [Theory]
    [InlineData("x + 1 > 100", "ARITHMETIC_EQUALITY_REWRITE", "x > 99")]
    [InlineData("101 > x + 1", "ARITHMETIC_EQUALITY_REWRITE", "x < 100")]
    [InlineData("1 + x = 100", "ARITHMETIC_EQUALITY_REWRITE", "x = 99")]
    [InlineData("x - 2 <= 98", "ARITHMETIC_EQUALITY_REWRITE", "x <= 100")]
    [InlineData("x LIKE 'Budapest'", "LIKE_LITERAL_REWRITE", "x = 'Budapest'")]
    [InlineData("x NOT LIKE 'a!_%' ESCAPE '!'", "NONE", "")]
    [InlineData("x LIKE 'a!_b' ESCAPE '!'", "LIKE_LITERAL_REWRITE", "x = 'a_b'")]
    [InlineData("x NOT LIKE 'a!%b' ESCAPE '!'", "LIKE_LITERAL_REWRITE", "x <> 'a%b'")]
    [InlineData("x LIKE 'a\\_b'", "LIKE_LITERAL_REWRITE", "x = 'a_b'")]
    [InlineData("x LIKE 'plain' ESCAPE ''", "LIKE_LITERAL_REWRITE", "x = 'plain'")]
    [InlineData("x = 1 OR x = 2", "OR_TO_IN_REWRITE", "x IN (1, 2)")]
    [InlineData("x IN (1)", "SINGLETON_IN_REWRITE", "x = 1")]
    [InlineData("x NOT IN ('a')", "SINGLETON_IN_REWRITE", "x <> 'a'")]
    [InlineData("x IN (1, 2, 1)", "DUPLICATE_IN_REWRITE", "x IN (1, 2)")]
    [InlineData("x NOT IN ('a', 'a', 'b')", "DUPLICATE_IN_REWRITE", "x NOT IN ('a', 'b')")]
    [InlineData("x::date = DATE '2024-02-29'", "DATE_CAST_RANGE_REWRITE", "2024-03-01")]
    [InlineData("DATE '2024-12-31' = x::date", "DATE_CAST_RANGE_REWRITE", "2025-01-01")]
    [InlineData("EXTRACT(YEAR FROM x) = 2024", "YEAR_RANGE_REWRITE", "2025-01-01")]
    [InlineData("2024 = date_part('year', x)", "YEAR_RANGE_REWRITE", "2024-01-01")]
    public void Rewrite_ProducesIndependentParseableCandidate(string predicate, string id, string fragment)
    {
        string sql = $"SELECT x FROM customers WHERE {predicate} ORDER BY x LIMIT 10";
        var findings = Service.Suggest(sql);
        if (id == "NONE") { Assert.Empty(findings); return; }
        var result = Assert.Single(findings);
        Assert.Equal(id, result.RuleId);
        Assert.Equal(sql, result.OriginalSql);
        Assert.Contains(fragment, result.SuggestedSql);
        Assert.Contains("ORDER BY x LIMIT 10", result.SuggestedSql);
        Assert.Null(Parser.Parse(result.SuggestedSql).Error);
        Assert.NotEmpty(result.Preconditions);
    }

    [Theory]
    [InlineData("> 0", false)]
    [InlineData("<> 0", false)]
    [InlineData(">= 1", false)]
    [InlineData("= 0", true)]
    [InlineData("<= 0", true)]
    [InlineData("< 1", true)]
    public void Count_PreservesCorrelationAndSelectsExistsPolarity(string comparison, bool negate)
    {
        string sql = $"SELECT c.customer_id FROM customers c WHERE (SELECT count(*) FROM orders o WHERE o.customer_id = c.customer_id) {comparison}";
        var result = Assert.Single(Service.Suggest(sql));
        var where = Parser.Parse(result.SuggestedSql).Value!.Stmts[0].Stmt.SelectStmt.WhereClause;
        if (negate) { Assert.Equal(BoolExprType.NotExpr, where.BoolExpr.Boolop); where = Assert.Single(where.BoolExpr.Args); }
        Assert.Equal(SubLinkType.ExistsSublink, where.SubLink.SubLinkType);
        Assert.Contains("o.customer_id = c.customer_id", result.SuggestedSql);
        Assert.Single(Assert.Single(new PostgreSqlQueryService().ParseSummary(sql).Statements).CountForExistenceOccurrences);
    }

    [Theory]
    [InlineData("x IN (NULL)")]
    [InlineData("x IN (1, NULL, 1)")]
    [InlineData("x IN (1, '1', 1)")]
    [InlineData("x = 1 OR y = 2")]
    [InlineData("x = 1 OR x IS NULL")]
    [InlineData("x = 1 OR x = $1")]
    [InlineData("x LIKE 'a%'")]
    [InlineData("x ILIKE 'a'")]
    [InlineData("x LIKE 'a_' ESCAPE ''")]
    [InlineData("x::date = DATE '9999-12-31'")]
    [InlineData("x::date = '2024-01-01'")]
    [InlineData("EXTRACT(MONTH FROM x) = 1")]
    [InlineData("EXTRACT(YEAR FROM x) = 0")]
    [InlineData("date_part('year', x) = $1")]
    [InlineData("public.date_part('year', x) = 2024")]
    [InlineData("(SELECT count(x) FROM orders) > 0")]
    [InlineData("(SELECT count(*) FROM orders GROUP BY x) > 0")]
    [InlineData("(SELECT count(*) FROM orders LIMIT 1) > 0")]
    [InlineData("(SELECT count(*) FROM orders WHERE random() > 0.5) > 0")]
    [InlineData("(SELECT count(*) FROM orders) > 1")]
    [InlineData("NOT (x IN (1))")]
    public void UnsafeOrUnsupportedPredicate_DoesNotProduceCandidate(string predicate)
        => Assert.Empty(Service.Suggest($"SELECT x FROM customers WHERE {predicate}"));

    [Fact]
    public void Having_PreservesWhereOrAndAggregateFilter()
    {
        const string sql = "SELECT customer_id, count(*) FROM orders WHERE status = 'a' OR status = 'b' GROUP BY customer_id HAVING customer_id > 100 AND count(*) > 2";
        var result = Assert.Single(new HavingGroupKeyRewriteRule().Suggest(sql));
        var query = Parser.Parse(result.SuggestedSql).Value!.Stmts[0].Stmt.SelectStmt;
        Assert.Equal(BoolExprType.AndExpr, query.WhereClause.BoolExpr.Boolop);
        Assert.Equal(BoolExprType.OrExpr, query.WhereClause.BoolExpr.Args[0].BoolExpr.Boolop);
        Assert.Equal("count", query.HavingClause.AExpr.Lexpr.FuncCall.Funcname[0].String.Sval);
    }

    [Theory]
    [InlineData("GROUP BY customer_id HAVING customer_id > 100 OR count(*) > 2")]
    [InlineData("GROUP BY ROLLUP(customer_id) HAVING customer_id > 100")]
    [InlineData("GROUP BY customer_id HAVING sum(random()) > 2 AND customer_id > 100")]
    public void Having_UnsupportedShape_IsNotMoved(string suffix)
        => Assert.Empty(new HavingGroupKeyRewriteRule().Suggest("SELECT customer_id FROM orders " + suffix));

    [Fact]
    public void NewCollectorsAndRules_ProduceFindingsWithLocations()
    {
        var statement = Assert.Single(new PostgreSqlQueryService().ParseSummary("SELECT x FROM t WHERE (x = 1 OR x = 2) AND y IN (3) AND z NOT IN ('a', 'a')").Statements);
        Assert.Equal(3, statement.PredicateSimplifications.Count);
        var results = new OrEqualityChainRule().Analyze(statement).Concat(new SingletonInRule().Analyze(statement)).Concat(new DuplicateInValuesRule().Analyze(statement)).ToList();
        Assert.Equal(3, results.Count);
        Assert.All(results, f => { Assert.Equal(1, f.StatementIndex); Assert.True(f.SourceOffset >= 0); Assert.NotEmpty(f.Recommendation); });
    }

    [Fact]
    public void Count_DetectionKeepsJoinSupportWhileRewriteRemainsConservative()
    {
        const string sql = "SELECT x FROM t WHERE (SELECT count(*) FROM a JOIN b ON a.id = b.id) > 0";
        Assert.Single(Assert.Single(new PostgreSqlQueryService().ParseSummary(sql).Statements).CountForExistenceOccurrences);
        Assert.Empty(Service.Suggest(sql));
    }
}
