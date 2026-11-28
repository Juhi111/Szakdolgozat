using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class RewriteMetadataTests
{
    [Theory]
    [InlineData("date", RewriteTypeRequirement.DateOrTimestamp, MetadataCheckStatus.Passed)]
    [InlineData("timestamp", RewriteTypeRequirement.DateOrTimestamp, MetadataCheckStatus.Passed)]
    [InlineData("timestamptz", RewriteTypeRequirement.DateOrTimestamp, MetadataCheckStatus.Failed)]
    [InlineData("interval", RewriteTypeRequirement.DateOrTimestamp, MetadataCheckStatus.Failed)]
    [InlineData("int8", RewriteTypeRequirement.Integer, MetadataCheckStatus.Passed)]
    [InlineData("numeric", RewriteTypeRequirement.Integer, MetadataCheckStatus.Failed)]
    [InlineData("varchar", RewriteTypeRequirement.Text, MetadataCheckStatus.Passed)]
    [InlineData("bpchar", RewriteTypeRequirement.Text, MetadataCheckStatus.Failed)]
    public void Evaluate_OnlySupportedBuiltinTypesPass(string type, RewriteTypeRequirement requirement, MetadataCheckStatus expected)
    {
        var result = RewriteMetadataEvaluator.Evaluate(requirement, Column(type));
        Assert.Equal(expected, result.Status);
        Assert.Equal(expected == MetadataCheckStatus.Failed, result.BlocksExecution);
        Assert.Contains("eredményazonosság nincs igazolva", result.Description);
    }

    [Fact]
    public void MissingDomainAndCustomTypes_RemainUnknown()
    {
        Assert.Equal(MetadataCheckStatus.Unknown, RewriteMetadataEvaluator.Evaluate(RewriteTypeRequirement.Text, null).Status);
        Assert.Equal(MetadataCheckStatus.Unknown, RewriteMetadataEvaluator.Evaluate(RewriteTypeRequirement.Text, Column("text") with { IsDomain = true }).Status);
        Assert.Equal(MetadataCheckStatus.Unknown, RewriteMetadataEvaluator.Evaluate(RewriteTypeRequirement.Text, Column("text") with { TypeSchema = "custom" }).Status);
    }

    [Theory]
    [InlineData("SELECT c.x FROM lab.customers c WHERE EXTRACT(YEAR FROM c.x) = 2020", "lab", "customers", "x")]
    [InlineData("SELECT x FROM customers WHERE x + 1 = 100", null, "customers", "x")]
    [InlineData("SELECT x FROM lab.customers WHERE lab.customers.x LIKE 'a'", "lab", "customers", "x")]
    [InlineData("SELECT x FROM \"Ő\".\"Tábla\" AS \"C\" WHERE \"C\".\"Dátum\"::date = DATE '2024-01-01'", "Ő", "Tábla", "Dátum")]
    public void GeneratedSuggestion_ResolvesExactAffectedColumn(string sql, string? schema, string table, string column)
    {
        var suggestion = Assert.Single(new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault()).Suggest(sql));
        Assert.Equal(new DatabaseColumnReference(schema, table, column), suggestion.MetadataRequirement!.Column);
    }

    [Fact]
    public void DifferentPredicates_KeepSeparateColumnsAndRejectWrongAlias()
    {
        var suggestions = new ArithmeticEqualityRewriteRule().Suggest("SELECT x FROM lab.t c WHERE c.x + 1 = 2 AND c.y + 2 = 3");
        Assert.Equal(new[] { "x", "y" }, suggestions.Select(s => s.MetadataRequirement!.Column.Column));
        Assert.Empty(new ArithmeticEqualityRewriteRule().Suggest("SELECT x FROM lab.t c WHERE t.x + 1 = 2"));
        Assert.Equal("\"strange\"\"schema\".\"table.name\"", PostgreSqlMetadataService.RelationName(new("strange\"schema", "table.name", "x")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedMetadata_BlocksBothExplainModesBeforeExecution(bool analyze)
    {
        var fake = new FakeExplain();
        var service = Service(fake, new FakeMetadata { Value = Column("interval") });
        var suggestion = Suggestion();
        var error = await Assert.ThrowsAsync<RewriteMetadataException>(() => service.CompareAsync(Original(suggestion), suggestion, Options(), "secret", analyze));
        Assert.True(error.Check.BlocksExecution);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task FreshMetadataIsReadForEachRunAndCapturedInComparison()
    {
        var fake = new FakeExplain();
        var metadata = new FakeMetadata { Value = Column("date") };
        var service = Service(fake, metadata);
        var suggestion = Suggestion();
        var original = Original(suggestion);
        var result = await service.CompareAsync(original, suggestion, Options(), "secret", false);
        Assert.Equal(MetadataCheckStatus.Passed, result.MetadataCheck.Status);
        Assert.NotNull(result.MetadataCheck.CheckedAt);
        Assert.Contains("db", result.MetadataCheck.Source!);
        Assert.DoesNotContain("secret", result.MetadataCheck.Description);
        metadata.Value = Column("interval");
        await Assert.ThrowsAsync<RewriteMetadataException>(() => service.CompareAsync(original, suggestion, Options(), "secret", false));
        Assert.Equal(2, metadata.Calls);
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public async Task TimeoutIsUnknownButCancellationPropagates()
    {
        var metadata = new FakeMetadata { Error = new TimeoutException("secret details") };
        var service = Service(new(), metadata);
        var suggestion = Suggestion();
        var result = await service.CheckMetadataAsync(Original(suggestion), suggestion, Options(), "secret");
        Assert.Equal(MetadataCheckStatus.Unknown, result.Status);
        Assert.DoesNotContain("secret", result.Description);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CheckMetadataAsync(Original(suggestion), suggestion, Options(), "secret", cts.Token));
        Assert.Equal(1, metadata.Calls);
    }

    private static DatabaseColumnMetadata Column(string type) => new("lab", "customers", "x", "pg_catalog", type, type, false, true);
    private static DatabaseConnectionOptions Options() => new() { Host = "localhost", Database = "db", Username = "reader", Schema = "lab" };
    private static SqlRewriteSuggestion Suggestion() => Assert.Single(new YearRangeRewriteRule().Suggest("SELECT x FROM lab.customers WHERE EXTRACT(YEAR FROM x) = 2020"));
    private static CombinedAnalysisResult Original(SqlRewriteSuggestion suggestion) => new(new StatementPlanPair(new SqlStatementSummary { StatementIndex = 1 },
        new ExecutionPlan(1, new PlanNode { NodeType = "Seq Scan" }) { Origin = new PlanOrigin { Source = PlanSourceKind.Database, SqlText = suggestion.OriginalSql,
            StatementIndex = 1, Host = "localhost", Port = 5432, Database = "db", Username = "reader", Schema = "lab" } }), Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>());
    private static SqlRewriteComparisonService Service(FakeExplain explain, FakeMetadata metadata) => new(explain,
        new SqlAnalysisService(Array.Empty<ISqlAnalysisRule>()), new PlanAnalysisService(Array.Empty<IPlanAnalysisRule>()), metadata);
    private sealed class FakeMetadata : IDatabaseMetadataService
    {
        public DatabaseColumnMetadata? Value { get; set; }
        public Exception? Error { get; set; }
        public int Calls { get; private set; }
        public Task<DatabaseColumnMetadata?> GetColumnAsync(DatabaseConnectionOptions options, string password, DatabaseColumnReference column, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); Calls++; return Error is null ? Task.FromResult(Value) : Task.FromException<DatabaseColumnMetadata?>(Error); }
    }
    private sealed class FakeExplain : IDatabaseExplainService
    {
        public int Calls { get; private set; }
        public Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult("[{\"Plan\":{\"Node Type\":\"Seq Scan\"}}]"); }
        public Task<string> GetActualPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default) => GetEstimatedPlanAsync(options, password, sql, cancellationToken);
    }
}
