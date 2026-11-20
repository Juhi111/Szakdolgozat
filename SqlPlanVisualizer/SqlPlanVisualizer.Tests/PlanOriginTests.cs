using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public sealed class PlanOriginTests
{
    private const string Document = "-- árvíztűrő\r\nSELECT 1;\r\nSELECT 1;";

    [Fact]
    public void Matches_RequiresExactDocumentAndStatement_NotJustIdenticalSql()
    {
        var statements = new PostgreSqlQueryService().ParseSummary(Document).Statements;
        var second = statements[1];
        var origin = Origin(Document, second);
        Assert.True(PlanOriginMatcher.Matches(origin, Document, second));
        Assert.False(PlanOriginMatcher.Matches(origin, Document, statements[0]));
        Assert.False(PlanOriginMatcher.Matches(origin, Document + " ", second));
        Assert.False(PlanOriginMatcher.Matches(origin with { SqlText = "SELECT 2" }, Document, second));
        Assert.False(PlanOriginMatcher.Matches(origin with { Source = PlanSourceKind.JsonFile }, Document, second));
        Assert.False(PlanOriginMatcher.Matches(origin with { SqlText = null }, Document, second));
        Assert.False(PlanOriginMatcher.Matches(null, Document, second));
        second.TextLength = int.MaxValue;
        Assert.False(PlanOriginMatcher.Matches(origin, Document, second));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task History_RoundTripsOriginAndHonorsSourceOptOut(bool includeSources)
    {
        var statement = new PostgreSqlQueryService().ParseSummary(Document).Statements[1];
        var origin = Origin(Document, statement);
        var plan = new ExecutionPlan(1, new PlanNode { NodeType = "Result" }) { Origin = origin };
        var result = new CombinedAnalysisResult(new StatementPlanPair(statement, plan), Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>());
        var entry = new AnalysisHistoryMapper().Map(result, includeSources, Document, "[]");
        Assert.Equal(includeSources ? origin.SqlText : null, entry.PlanOrigin!.SqlText);
        Assert.Equal(origin.SqlText, plan.Origin!.SqlText); // Redaction must not mutate the live plan.
        Assert.Equal("lab", entry.PlanOrigin.Schema);
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var service = new AnalysisHistoryService();
            string path = await service.SaveAsync(entry, directory);
            var loaded = await service.LoadAsync(path);
            Assert.Equal(entry.PlanOrigin, loaded.PlanOrigin);
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            var savedOrigin = json.RootElement.GetProperty("PlanOrigin");
            Assert.False(savedOrigin.TryGetProperty("Password", out _));
            Assert.False(savedOrigin.TryGetProperty("ConnectionString", out _));
            if (!includeSources)
            {
                Assert.False(savedOrigin.TryGetProperty("SqlText", out _));
                Assert.False(json.RootElement.TryGetProperty("SqlText", out _));
                Assert.False(json.RootElement.TryGetProperty("ExplainJson", out _));
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ParsePlans_KeepsExplicitOrigin_AndDoesNotInventDatabaseOrigin()
    {
        const string json = """[{"Plan":{"Node Type":"Result"}}]""";
        var service = new PostgreSqlPlanService();
        Assert.Null(Assert.Single(service.ParsePlans(json)).Origin);
        var origin = new PlanOrigin { Source = PlanSourceKind.JsonFile, FileName = "plan.json", CapturedAt = DateTimeOffset.UtcNow };
        Assert.Same(origin, Assert.Single(service.ParsePlans(json, origin)).Origin);
    }

    private static PlanOrigin Origin(string document, SqlStatementSummary statement) => new()
    {
        Source = PlanSourceKind.Database,
        Mode = PlanCaptureMode.Actual,
        CapturedAt = DateTimeOffset.UtcNow,
        StatementIndex = statement.StatementIndex,
        SqlText = document.Substring(statement.TextStart!.Value, statement.TextLength),
        DocumentHash = PlanOriginMatcher.HashDocument(document),
        Host = "localhost", Port = 5432, Database = "test", Schema = "lab", Username = "tester"
    };
}
