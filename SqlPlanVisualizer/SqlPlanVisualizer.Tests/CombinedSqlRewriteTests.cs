using PgSqlParser;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class CombinedSqlRewriteTests
{
    private static SqlRewriteService Engine() => new(SqlRewriteRuleFactory.CreateDefault());

    [Fact]
    public void AllOccurrencesAndRules_AreAppliedToOneCandidateUntilStable()
    {
        const string sql = "SELECT 'árvíz' AS note, c.customer_id FROM szakdoga_lab.customers c WHERE c.customer_id + 10 > 100 AND c.customer_id - 2 < 50000 AND c.city LIKE 'Budapest' AND c.country_code IN ('HU', 'HU') AND c.customer_status IN ('active') ORDER BY c.customer_id LIMIT 100";
        var suggestion = Engine().SuggestCombined(sql)!;
        Assert.Equal(6, suggestion.AppliedSteps.Count);
        Assert.Equal(sql, suggestion.OriginalSql);
        Assert.Contains("customer_id > 90", suggestion.SuggestedSql);
        Assert.Contains("customer_id < 50002", suggestion.SuggestedSql);
        Assert.Contains("city = 'Budapest'", suggestion.SuggestedSql);
        Assert.Contains("country_code = 'HU'", suggestion.SuggestedSql);
        Assert.Contains("customer_status = 'active'", suggestion.SuggestedSql);
        Assert.Contains("ORDER BY c.customer_id LIMIT 100", suggestion.SuggestedSql);
        Assert.Null(Parser.Parse(suggestion.SuggestedSql).Error);
        Assert.Null(Engine().SuggestCombined(suggestion.SuggestedSql));
        string current = sql;
        foreach (var step in suggestion.AppliedSteps)
        {
            Assert.Equal(current, step.OriginalSql);
            current = step.SuggestedSql;
        }
        Assert.Equal(current, suggestion.SuggestedSql);
    }

    [Fact]
    public void HavingMove_EnablesFurtherWhereRewrite()
    {
        var suggestion = Engine().SuggestCombined("SELECT x, count(*) FROM t WHERE x IN (1, 1) GROUP BY x HAVING x > 0 AND x < 100")!;
        Assert.Equal(4, suggestion.AppliedSteps.Count);
        Assert.DoesNotContain("HAVING", suggestion.SuggestedSql);
        Assert.Contains("x = 1", suggestion.SuggestedSql);
    }

    [Fact]
    public void UnsupportedPatterns_ArePreservedRatherThanReportedAsFixed()
    {
        const string sql = "SELECT DISTINCT * FROM szakdoga_lab.customers c WHERE lower(c.email) LIKE '%gmail.com' AND upper(c.city) LIKE 'BUDAPEST' AND c.customer_id + 10 > 100 AND CAST(c.registered_at AS date) >= DATE '2023-01-01' AND EXTRACT(YEAR FROM c.registered_at) <= 2026 AND c.first_name || ' ' || c.last_name LIKE '%a%' AND CASE WHEN c.marketing_opt_in THEN 1 ELSE 0 END = 1 LIMIT 100";
        var suggestion = Engine().SuggestCombined(sql)!;
        Assert.Equal(5, suggestion.AppliedSteps.Count);
        Assert.Contains("customer_id > 90", suggestion.SuggestedSql);
        Assert.Contains("upper(c.city) = 'BUDAPEST'", suggestion.SuggestedSql);
        Assert.Contains("marketing_opt_in IS TRUE", suggestion.SuggestedSql);
        Assert.Contains("SELECT DISTINCT *", suggestion.SuggestedSql);
        Assert.Contains("LIMIT 100", suggestion.SuggestedSql);
    }

    [Fact]
    public void DocumentSelection_DoesNotRewriteOtherStatements()
    {
        const string sql = "SELECT 1; -- ékezet\nSELECT x FROM t WHERE x + 1 = 2 AND y + 2 = 3; SELECT 3;";
        var statement = new PostgreSqlQueryService().ParseSummary(sql).Statements[1];
        var suggestion = Assert.Single(Engine().SuggestCombinedStatement(sql, statement));
        Assert.Equal(2, suggestion.AppliedSteps.Count);
        Assert.DoesNotContain("SELECT 1", suggestion.OriginalSql);
        Assert.DoesNotContain("SELECT 3", suggestion.SuggestedSql);
    }

    [Fact]
    public void CyclicRules_FailInsteadOfReturningAnIncompleteCandidate()
    {
        Assert.Throws<InvalidOperationException>(() => new SqlRewriteService([new Flip()]).SuggestCombined("a"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaterStepTypeFailure_BlocksTheWholeCandidate(bool analyze)
    {
        var suggestion = Engine().SuggestCombined("SELECT x FROM lab.t WHERE x + 1 = 2 AND y + 2 = 3")!;
        var explain = new FakeExplain();
        var metadata = new FakeMetadata { FailColumn = "y" };
        var service = Service(explain, metadata);
        await Assert.ThrowsAsync<RewriteMetadataException>(() => service.CompareAsync(Original(suggestion), suggestion, Options(), "", analyze));
        Assert.Equal(new[] { "x", "y" }, metadata.Columns);
        Assert.Equal(0, explain.Calls);
    }

    [Fact]
    public async Task SuccessfulComposition_ChecksAllColumnsAndExecutesOnlyFinalSql()
    {
        var suggestion = Engine().SuggestCombined("SELECT x FROM lab.t WHERE x + 1 = 2 AND y + 2 = 3")!;
        var explain = new FakeExplain();
        var metadata = new FakeMetadata();
        var comparison = await Service(explain, metadata).CompareAsync(Original(suggestion), suggestion, Options(), "", false);
        Assert.Equal(new[] { "x", "y" }, metadata.Columns);
        Assert.Equal(1, explain.Calls);
        Assert.Equal(suggestion.SuggestedSql, explain.Sql);
        Assert.Equal(MetadataCheckStatus.Passed, comparison.MetadataCheck.Status);
    }

    [Fact]
    public async Task ModifiedFinalSqlOrBrokenChain_CannotBypassPreflight()
    {
        var suggestion = Engine().SuggestCombined("SELECT x FROM lab.t WHERE x + 1 = 2 AND y + 2 = 3")!;
        var explain = new FakeExplain();
        var service = Service(explain, new());
        var changed = suggestion with { SuggestedSql = "SELECT 42" };
        await Assert.ThrowsAsync<RewriteMetadataException>(() => service.CompareAsync(Original(changed), changed, Options(), "", false));
        changed = suggestion with { AppliedSteps = suggestion.AppliedSteps.Reverse().ToArray() };
        await Assert.ThrowsAsync<RewriteMetadataException>(() => service.CompareAsync(Original(changed), changed, Options(), "", false));
        Assert.Equal(0, explain.Calls);
    }

    private sealed class Flip : ISqlRewriteRule
    {
        public IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql) => [new() { RuleId = "flip", Title = "flip", OriginalSql = sql, SuggestedSql = sql == "a" ? "b" : "a", Explanation = "", Preconditions = "" }];
    }
    private static DatabaseConnectionOptions Options() => new() { Database = "db", Username = "reader", Schema = "lab" };
    private static CombinedAnalysisResult Original(SqlRewriteSuggestion suggestion) => new(new(new() { StatementIndex = 1 }, new(1, new())
        { Origin = new() { Source = PlanSourceKind.Database, SqlText = suggestion.OriginalSql, StatementIndex = 1, Host = "localhost", Port = 5432, Database = "db", Username = "reader", Schema = "lab" } }), [], []);
    private static SqlRewriteComparisonService Service(FakeExplain explain, FakeMetadata metadata) => new(explain, new([]), new([]), metadata);
    private sealed class FakeMetadata : IDatabaseMetadataService
    {
        public string? FailColumn { get; init; }
        public List<string> Columns { get; } = [];
        public Task<DatabaseColumnMetadata?> GetColumnAsync(DatabaseConnectionOptions options, string password, DatabaseColumnReference column, CancellationToken cancellationToken = default)
        {
            Columns.Add(column.Column);
            string type = column.Column == FailColumn ? "text" : "int8";
            return Task.FromResult<DatabaseColumnMetadata?>(new("lab", "t", column.Column, "pg_catalog", type, type, false, false));
        }
    }
    private sealed class FakeExplain : IDatabaseExplainService
    {
        public int Calls { get; private set; }
        public string? Sql { get; private set; }
        public Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default)
        { Calls++; Sql = sql; return Task.FromResult("[{\"Plan\":{\"Node Type\":\"Seq Scan\"}}]"); }
        public Task<string> GetActualPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default) => GetEstimatedPlanAsync(options, password, sql, cancellationToken);
    }
}
