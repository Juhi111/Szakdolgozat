using Npgsql;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Core.Models.History;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class AutomaticMetadataTests
{
    [Fact]
    public async Task Snapshot_ReusesColumnsAndRejectsDifferentDatabase()
    {
        var options = new DatabaseConnectionOptions { Database = "db", Username = "u", Schema = "lab" };
        var table = new DatabaseTableMetadata { Schema = "lab", Table = "t", Columns = [new() { Name = "id", TypeName = "int8", TypeSchema = "pg_catalog", DisplayType = "bigint", IsNullable = false }] };
        var reader = new AnalysisMetadataSnapshotReader(options, new Dictionary<DatabaseTableReference, DatabaseTableMetadata?> { [new("lab", "t")] = table });
        var column = await reader.GetColumnAsync(options, "", new("lab", "t", "id"));
        Assert.Equal("int8", column!.TypeName); Assert.False(column.IsNullable);
        Assert.Null(await reader.GetColumnAsync(options, "", new("lab", "t", "missing")));
        options.Database = "other";
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.GetColumnAsync(options, "", new("lab", "t", "id")));
    }

    [Fact]
    public void Overview_MergesExactPlanEvidenceAndKeepsOtherNodes()
    {
        var entry = new AnalysisHistoryEntry
        {
            CombinedFindings = [new() { RuleId = "ORDER_BY_WITH_SORT_SPILL", Title = "Combined", PlanObservation = "sort 1", Description = "x", Recommendation = "y" }],
            PlanFindings = [new() { RuleId = "SORT_SPILL", Title = "sort 1", Description = "sort 1" }, new() { RuleId = "SORT_SPILL", Title = "sort 2", Description = "sort 2" }]
        };
        var overview = AnalysisOverview.Create(entry);
        Assert.Equal(2, overview.Count);
        Assert.Contains(overview, i => i.Title == "sort 2");
        Assert.DoesNotContain(overview, i => i.Title == "sort 1");
    }

    [Fact]
    public void AllCurrentRuleIds_HaveShortReadableText()
    {
        string[] ids = ["SELECT_STAR", "DISTINCT_USAGE", "NULL_COMPARISON", "LIMIT_WITHOUT_ORDER", "LIKE_WITHOUT_WILDCARD", "LEADING_WILDCARD_SEARCH", "FUNCTION_ON_COLUMN", "CAST_ON_COLUMN", "ARITHMETIC_ON_COLUMN", "NOT_IN_SUBQUERY", "DATE_CAST_IN_PREDICATE", "YEAR_EXTRACTION_IN_PREDICATE", "CONCATENATION_IN_PREDICATE", "TEXT_CAST_IN_PREDICATE", "OPTIONAL_PARAMETER_FILTER", "CASE_IN_PREDICATE", "NOT_IN_WITH_NULL_LITERAL", "COUNT_FOR_EXISTENCE", "ROW_FILTER_IN_HAVING", "POSSIBLE_CARTESIAN_PRODUCT", "LARGE_OFFSET", "UNION_DEDUPLICATION", "NATURAL_JOIN_USAGE", "RANDOM_ORDERING", "OR_EQUALITY_CHAIN", "SINGLETON_IN", "DUPLICATE_IN_VALUES", "ROW_ESTIMATE_MISMATCH", "SORT_SPILL", "HASH_MULTIPLE_BATCHES", "HASH_AGGREGATE_SPILL", "HIGH_FILTER_REMOVAL", "LOSSY_BITMAP_HEAP_SCAN", "HIGH_HEAP_FETCHES", "REPEATED_SEQUENTIAL_SCAN", "LARGE_SORT_FOR_LIMIT", "PARALLEL_WORKER_SHORTAGE", "INTERMEDIATE_STORAGE_SPILL", "DISTINCT_WITH_SPILL", "UNION_WITH_SPILL", "GROUP_BY_WITH_SPILL", "ORDERED_LIMIT_WITH_LARGE_SORT", "ORDER_BY_WITH_SORT_SPILL", "FUNCTION_FILTER_WITH_HIGH_REMOVAL", "LEADING_WILDCARD_WITH_HIGH_REMOVAL", "LIKE_WITHOUT_WILDCARD_WITH_HIGH_REMOVAL", "CAST_FILTER_WITH_HIGH_REMOVAL", "ARITHMETIC_FILTER_WITH_HIGH_REMOVAL", "YEAR_EXTRACTION_WITH_HIGH_REMOVAL", "CONCATENATION_WITH_HIGH_REMOVAL", "OPTIONAL_PARAMETER_WITH_HIGH_REMOVAL", "CASE_FILTER_WITH_HIGH_REMOVAL", "RANDOM_ORDERING_WITH_SPILL", "LARGE_OFFSET_WITH_LARGE_SORT", "HAVING_FILTER_WITH_HIGH_REMOVAL"];
        foreach (var id in ids)
        {
            Assert.True(FindingTextCatalog.Contains(id), id);
            var text = FindingTextCatalog.Get(id, "", "");
            Assert.InRange(text.Summary.Length, 10, 320); Assert.InRange(text.NextStep.Length, 10, 320);
        }
    }

    [Fact]
    public void UnknownRule_KeepsOriginalText()
    {
        var text = FindingTextCatalog.Get("CUSTOM", "original", "next");
        Assert.Equal("original", text.Summary); Assert.Equal("next", text.NextStep);
    }

    [PostgreSqlMetadataFact]
    public async Task LiveCatalog_ReadsAllMetadataAndHandlesMissingRelations()
    {
        string connectionString = Environment.GetEnvironmentVariable("SQLPLAN_METADATA_TEST_CONNECTION")!;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var options = new DatabaseConnectionOptions { Host = builder.Host!, Port = builder.Port, Database = builder.Database!, Username = builder.Username!, Schema = "meta_test_" + Guid.NewGuid().ToString("N"), TlsMode = DatabaseTlsMode.Disable };
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        string schema = options.Schema; // Generated only by this test; never user-provided SQL.
        try
        {
            await using var setup = new NpgsqlCommand($"""
                CREATE SCHEMA {schema};
                CREATE TABLE {schema}.parent (id bigint PRIMARY KEY);
                CREATE TABLE {schema}.sample (id bigint PRIMARY KEY, parent_id bigint REFERENCES {schema}.parent(id), email varchar(100), value integer CHECK (value > 0));
                CREATE INDEX sample_email ON {schema}.sample (lower(email)) INCLUDE (id) WHERE email IS NOT NULL;
                CREATE STATISTICS {schema}.sample_stats ON email, value FROM {schema}.sample;
                INSERT INTO {schema}.sample (id, email, value) SELECT i, 'user' || i, i FROM generate_series(1,100) i;
                ANALYZE {schema}.sample;
                """, connection);
            await setup.ExecuteNonQueryAsync();
            var service = new PostgreSqlIndexMetadataService();
            var snapshot = await service.LoadSnapshotAsync(options, builder.Password ?? "", [new(schema, "sample"), new(schema, "missing")]);
            var table = await snapshot.GetTableAsync(options, "", new(schema, "sample"));
            Assert.NotNull(table);
            Assert.Equal(4, table.Columns.Count);
            Assert.Contains(table.Keys, k => k.Kind == "f"); Assert.Contains(table.Keys, k => k.Kind == "c");
            Assert.Contains(table.Indexes, i => i.Name == "sample_email" && i.Predicate is not null && i.Elements.Any(e => e.Included));
            Assert.NotEmpty(table.ExtendedStatistics); Assert.NotEmpty(table.ColumnStatistics);
            Assert.True(table.Statistics.TotalBytes > 0); Assert.Equal(100d, table.Statistics.EstimatedRows);
            Assert.Contains("work_mem", table.Settings.Keys); Assert.Contains("plpgsql", table.Extensions);
            Assert.False(table.Columns.Single(c => c.Name == "id").IsNullable);
            Assert.Null(await snapshot.GetTableAsync(options, "", new(schema, "missing")));
            Assert.Equal("varchar", (await snapshot.GetColumnAsync(options, "", new(schema, "sample", "email")))!.TypeName);
        }
        finally { await using var cleanup = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE", connection); await cleanup.ExecuteNonQueryAsync(); }
    }
    [PostgreSqlMetadataFact]
    public async Task LiveCatalog_RespectsColumnPermissionsAndRecoversAfterDeniedSchema()
    {
        string connectionString = Environment.GetEnvironmentVariable("SQLPLAN_METADATA_TEST_CONNECTION")!;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        string suffix = Guid.NewGuid().ToString("N"), schema = "allowed_" + suffix, hidden = "hidden_" + suffix, role = "reader_" + suffix;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        try
        {
            await using var setup = new NpgsqlCommand($"CREATE ROLE {role} LOGIN; CREATE SCHEMA {schema}; CREATE SCHEMA {hidden}; CREATE TABLE {schema}.t (id integer, secret text); CREATE TABLE {hidden}.t(id integer); GRANT USAGE ON SCHEMA {schema} TO {role}; GRANT SELECT(id) ON {schema}.t TO {role};", connection);
            await setup.ExecuteNonQueryAsync();
            var options = new DatabaseConnectionOptions { Host = builder.Host!, Port = builder.Port, Database = builder.Database!, Username = role, Schema = schema, TlsMode = DatabaseTlsMode.Disable };
            var snapshot = await new PostgreSqlIndexMetadataService().LoadSnapshotAsync(options, "", [new(hidden, "t"), new(schema, "t")]);
            Assert.Null(await snapshot.GetTableAsync(options, "", new(hidden, "t")));
            var table = await snapshot.GetTableAsync(options, "", new(schema, "t"));
            Assert.NotNull(table);
            Assert.Equal("id", Assert.Single(table.Columns).Name);
            Assert.Null(await snapshot.GetColumnAsync(options, "", new(schema, "t", "secret")));
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE; DROP SCHEMA IF EXISTS {hidden} CASCADE; DROP ROLE IF EXISTS {role}", connection);
            await cleanup.ExecuteNonQueryAsync();
        }
    }

}

public sealed class PostgreSqlMetadataFactAttribute : FactAttribute
{
    public PostgreSqlMetadataFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQLPLAN_METADATA_TEST_CONNECTION")))
            Skip = "Set SQLPLAN_METADATA_TEST_CONNECTION to an isolated PostgreSQL 15+ test database. This test creates and drops its own schema.";
    }
}
