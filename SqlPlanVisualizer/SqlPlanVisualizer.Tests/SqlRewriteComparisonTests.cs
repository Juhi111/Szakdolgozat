using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class SqlRewriteComparisonTests
{
    private const string Sql = "SELECT customer_id FROM orders WHERE customer_id + 1 = 101";
    private static DatabaseConnectionOptions Options() => new() { Host = "localhost", Database = "test", Schema = "lab", Username = "reader" };
    private static SqlRewriteSuggestion Suggestion() => new() { RuleId = "test", Title = "test", OriginalSql = Sql,
        SuggestedSql = "SELECT customer_id FROM orders WHERE customer_id = 100", Explanation = "test", Preconditions = "test" };
    private static CombinedAnalysisResult Original(PlanCaptureMode mode = PlanCaptureMode.Actual) => new(
        new StatementPlanPair(new SqlStatementSummary { StatementIndex = 2 }, new ExecutionPlan(1, new PlanNode { NodeType = "Seq Scan" })
        {
            ExecutionTime = 20,
            Origin = new PlanOrigin { Source = PlanSourceKind.Database, Mode = mode, SqlText = Sql, StatementIndex = 2,
                Host = "localhost", Port = 5432, Database = "test", Schema = "lab", Username = "reader" }
        }), Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>());
    private static SqlRewriteComparisonService Service(FakeExplain fake) => new(fake,
        new SqlAnalysisService(Array.Empty<ISqlAnalysisRule>()), new PlanAnalysisService(Array.Empty<IPlanAnalysisRule>()));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Compare_CapturesSourceAndKeepsOriginalSeparate(bool analyze)
    {
        var original = Original();
        var fake = new FakeExplain();
        var result = await Service(fake).CompareAsync(original, Suggestion(), Options(), "secret", analyze);
        Assert.Equal(analyze, fake.Actual);
        Assert.Equal(Suggestion().SuggestedSql, fake.Sql);
        Assert.Same(original, result.Original);
        Assert.Equal("Seq Scan", original.Pair.Plan.Root.NodeType);
        Assert.Equal("Index Scan", result.SuggestedPlan.Root.NodeType);
        Assert.Equal(Suggestion().SuggestedSql, result.SuggestedPlan.Origin!.SqlText);
        Assert.Equal("test", result.SuggestedPlan.Origin.Database);
        Assert.NotNull(result.SuggestedPlan.Origin.DocumentHash);
        Assert.Contains("nincs ellenőrizve", result.EquivalenceStatus);
        if (analyze) Assert.Contains("10", result.TimingAssessment);
        else Assert.Contains("csak két mért", result.TimingAssessment);
    }

    [Theory]
    [InlineData("host")]
    [InlineData("port")]
    [InlineData("database")]
    [InlineData("schema")]
    [InlineData("username")]
    [InlineData("sql")]
    [InlineData("file")]
    public async Task Compare_MismatchedSource_DoesNotContactDatabase(string change)
    {
        var options = Options();
        var suggestion = Suggestion();
        var original = Original();
        switch (change)
        {
            case "host": options.Host = "other"; break;
            case "port": options.Port = 5433; break;
            case "database": options.Database = "other"; break;
            case "schema": options.Schema = "other"; break;
            case "username": options.Username = "other"; break;
            case "sql": suggestion = suggestion with { OriginalSql = "SELECT 1" }; break;
            case "file": original = new(new StatementPlanPair(original.Pair.Statement, new ExecutionPlan(1, new PlanNode())),
                Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>()); break;
        }
        var fake = new FakeExplain();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(fake).CompareAsync(original, suggestion, options, "secret", true));
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task Compare_CancellationAndMalformedResponse_DoNotReplaceOriginal()
    {
        var fake = new FakeExplain();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(fake).CompareAsync(Original(), Suggestion(), Options(), "secret", true, cts.Token));
        Assert.Equal(0, fake.Calls);
        fake.Json = "[]";
        var original = Original();
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => Service(fake).CompareAsync(original, Suggestion(), Options(), "secret", false));
        Assert.Equal("Seq Scan", original.Pair.Plan.Root.NodeType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Timing_InvalidValuesNeverProduceSpeedup(double time)
    {
        var result = new SqlRewriteComparison(Original(), Suggestion(), new ExecutionPlan(1, new PlanNode())
        { ExecutionTime = time, Origin = new PlanOrigin { Mode = PlanCaptureMode.Actual } }, Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>());
        Assert.Contains("Nincs két pozitív", result.TimingAssessment);
    }

    [Theory]
    [InlineData(10, "gyorsabb")]
    [InlineData(40, "lassabb")]
    [InlineData(20, "közel azonos")]
    [InlineData(20.001, "közel azonos")]
    [InlineData(0, "Nincs elegendő")]
    [InlineData(double.NaN, "Nincs elegendő")]
    [InlineData(double.PositiveInfinity, "Nincs elegendő")]
    public void TimingSummary_DescribesMeasuredDirectionAndInvalidData(double time, string expected)
    {
        var result = new SqlRewriteComparison(Original(), Suggestion(), new ExecutionPlan(1, new PlanNode())
        { ExecutionTime = time, Origin = new PlanOrigin { Mode = PlanCaptureMode.Actual } }, [], []);
        Assert.Contains(expected, result.TimingSummary);
        Assert.DoesNotContain("localhost", result.TimingSummary);
    }

    [Fact]
    public void TimingSummary_DoesNotPresentEstimatesAsMeasurements()
    {
        var result = new SqlRewriteComparison(Original(), Suggestion(), new ExecutionPlan(1, new PlanNode())
        { ExecutionTime = 1, Origin = new PlanOrigin { Mode = PlanCaptureMode.Estimated } }, [], []);
        Assert.Contains("futtatás és mérés szükséges", result.TimingSummary);
        Assert.DoesNotContain("gyorsabb", result.TimingSummary);
    }

    private sealed class FakeExplain : IDatabaseExplainService
    {
        public int Calls { get; private set; }
        public bool Actual { get; private set; }
        public string? Sql { get; private set; }
        public string Json { get; set; } = "[{\"Plan\":{\"Node Type\":\"Index Scan\"},\"Execution Time\":2}]";
        private Task<string> Run(string sql, bool actual, CancellationToken token) { token.ThrowIfCancellationRequested(); Calls++; Sql = sql; Actual = actual; return Task.FromResult(Json); }
        public Task<string> GetActualPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default) => Run(sql, true, cancellationToken);
        public Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default) => Run(sql, false, cancellationToken);
    }
}
