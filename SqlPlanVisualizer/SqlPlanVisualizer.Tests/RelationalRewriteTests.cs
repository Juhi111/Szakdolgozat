using Npgsql;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class RelationalRewriteTests
{
    public const string NotIn = "SELECT c.id FROM lab.customers c WHERE c.id NOT IN (SELECT o.customer_id FROM lab.orders o)";
    public const string Anti = "SELECT c.id, c.name FROM lab.customers c LEFT JOIN lab.orders o ON o.customer_id = c.id WHERE o.id IS NULL";
    public const string Distinct = "SELECT DISTINCT c.id, c.name FROM lab.customers c";
    public const string Join = "SELECT DISTINCT c.id, c.name FROM lab.customers c JOIN lab.orders o ON o.customer_id = c.id";
    internal static DatabaseTableMetadata Table(string name, bool? nullable = false, bool valid = true, bool deferrable = false,
        string kind = "r", bool? descendants = false, bool? rls = false, bool defaultOperatorClass = true, string type = "int8", bool composite = false) => new()
    {
        Schema = "lab", Table = name, RelationKind = kind, HasDescendants = descendants, RowSecurity = rls,
        Columns = [new() { Name = "id", TypeSchema = "pg_catalog", TypeName = type, IsNullable = nullable },
            new() { Name = "customer_id", TypeSchema = "pg_catalog", TypeName = type, IsNullable = nullable },
            new() { Name = "name", TypeSchema = "pg_catalog", TypeName = "text", IsNullable = true }],
        Keys = [new() { Name = "pk", Kind = "p", Validated = true, Deferrable = deferrable, Columns = composite ? ["id", "customer_id"] : ["id"], IndexName = "pk" }],
        Indexes = [new() { Name = "pk", IsPrimary = true, IsUnique = true, IsValid = valid, IsLive = true, IsReady = true, Method = "btree",
            HasDefaultOperatorClasses = defaultOperatorClass, Elements = composite ? [new() { Position = 1, Column = "id" }, new() { Position = 2, Column = "customer_id" }] : [new() { Position = 1, Column = "id" }] }]
    };
    internal static IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql, params DatabaseTableMetadata[] tables) =>
        new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault(tables.Length == 0 ? [Table("customers"), Table("orders")] : tables)).Suggest(sql)
            .Where(s => s.RequiresRelationalMetadata).ToArray();

    [Theory]
    [InlineData("SELECT DISTINCT c.* FROM lab.customers c")]
    [InlineData("SELECT DISTINCT c.id FROM lab.customers c ORDER BY c.id")]
    [InlineData("SELECT DISTINCT c.id FROM lab.customers c LIMIT 1")]
    [InlineData("SELECT DISTINCT c.id FROM lab.customers c OFFSET 1")]
    [InlineData("SELECT DISTINCT c.id FROM lab.customers c WHERE c.id > 1")]
    public void Distinct_WithFiltersOrderingAndStar_UsesPrimaryKey(string sql) =>
        Assert.Equal("REDUNDANT_DISTINCT_REWRITE", Assert.Single(Suggest(sql)).RuleId);

    [Theory]
    [InlineData(NotIn, "NOT_IN_EXISTS_REWRITE")]
    [InlineData(Anti, "LEFT_JOIN_ANTI_REWRITE")]
    [InlineData(Distinct, "REDUNDANT_DISTINCT_REWRITE")]
    [InlineData(Join, "DISTINCT_JOIN_EXISTS_REWRITE")]
    public void SupportedCases_CreateSeparateSqlAndReparse(string sql, string id)
    {
        var suggestion = Assert.Single(Suggest(sql));
        Assert.Equal(id, suggestion.RuleId); Assert.Equal(sql, suggestion.OriginalSql); Assert.NotEqual(sql, suggestion.SuggestedSql);
        Assert.True(new PostgreSqlQueryService().ParseSummary(suggestion.SuggestedSql).IsSuccess);
        Assert.Empty(new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault()).Suggest(sql));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(null, false)]
    [InlineData(false, null)]
    public void NotIn_RequiresBothSidesNotNull(bool? outer, bool? inner) =>
        Assert.Empty(Suggest(NotIn, Table("customers", outer), Table("orders", inner)));

    [Fact]
    public void Anti_RequiresNonNullTestColumnButDoesNotRequireUniqueLeftRows()
    {
        Assert.Empty(Suggest(Anti, Table("customers"), Table("orders", true)));
        Assert.Single(Suggest(Anti, Table("customers", true, valid: false), Table("orders")));
    }

    [Theory]
    [InlineData("r", true, false)]
    [InlineData("r", false, true)]
    [InlineData("r", null, false)]
    [InlineData("r", false, null)]
    [InlineData("p", false, false)]
    [InlineData("v", false, false)]
    [InlineData("", false, false)]
    public void UnsupportedTableScope_DeclinesAll(string kind, bool? descendants, bool? rls)
    {
        foreach (var sql in new[] { NotIn, Anti, Distinct, Join })
            Assert.Empty(Suggest(sql, Table("customers", kind: kind, descendants: descendants, rls: rls), Table("orders")));
    }

    [Theory]
    [InlineData(false, false, true, "int8")]
    [InlineData(true, true, true, "int8")]
    [InlineData(true, false, false, "int8")]
    [InlineData(true, false, true, "text")]
    public void Distinct_RequiresUsableImmediateBuiltinPrimaryKey(bool valid, bool deferrable, bool defaultOpclass, string type)
    {
        foreach (var sql in new[] { Distinct, Join })
            Assert.Empty(Suggest(sql, Table("customers", valid: valid, deferrable: deferrable, defaultOperatorClass: defaultOpclass, type: type), Table("orders")));
    }

    [Fact]
    public void CompositeKey_RequiresEveryKeyColumn()
    {
        Assert.Empty(Suggest(Distinct, Table("customers", composite: true)));
        Assert.Single(Suggest("SELECT DISTINCT c.id, c.customer_id FROM lab.customers c", Table("customers", composite: true)));
    }

    [Theory]
    [InlineData("SELECT DISTINCT c.name FROM lab.customers c")]
    [InlineData("SELECT DISTINCT ON (c.id) c.id FROM lab.customers c")]
    [InlineData("SELECT DISTINCT c.id + 1 FROM lab.customers c")]
    [InlineData("SELECT DISTINCT c.id, random() FROM lab.customers c")]
    [InlineData("WITH x AS (SELECT 1) SELECT DISTINCT c.id FROM lab.customers c")]
    [InlineData("SELECT DISTINCT c.id FROM customers c")]
    [InlineData("SELECT DISTINCT id FROM lab.customers")]
    [InlineData("SELECT c.id FROM lab.customers c WHERE c.id NOT IN (SELECT c.customer_id FROM lab.orders c)")]
    [InlineData("SELECT c.id FROM lab.customers c WHERE c.id NOT IN (SELECT o.customer_id FROM lab.orders o WHERE o.id > 1)")]
    [InlineData("SELECT c.id FROM lab.customers c WHERE c.id NOT IN (SELECT o.customer_id FROM lab.orders o LIMIT 1)")]
    [InlineData("SELECT c.id FROM lab.customers c WHERE c.id NOT IN (SELECT o.customer_id + 0 FROM lab.orders o)")]
    [InlineData("SELECT c.id FROM lab.customers c WHERE c.id NOT IN (SELECT o.customer_id FROM lab.orders o) OR c.id=1")]
    [InlineData("SELECT c.id, o.id FROM lab.customers c LEFT JOIN lab.orders o ON c.id=o.customer_id WHERE o.id IS NULL")]
    [InlineData("SELECT c.id FROM lab.customers c LEFT JOIN lab.orders o ON c.id=o.customer_id WHERE o.name IS NULL")]
    [InlineData("SELECT c.id FROM lab.customers c LEFT JOIN lab.orders o ON c.id<>o.customer_id WHERE o.id IS NULL")]
    [InlineData("SELECT DISTINCT c.id, o.id FROM lab.customers c JOIN lab.orders o ON c.id=o.customer_id")]
    [InlineData("SELECT DISTINCT c.id FROM lab.customers c JOIN lab.orders o ON c.id=o.customer_id WHERE o.id > 1")]
    [InlineData("SELECT DISTINCT c.name FROM lab.customers c JOIN lab.orders o ON c.id=o.customer_id")]
    public void UnsupportedShape_DoesNotGuess(string sql) => Assert.Empty(Suggest(sql));

    [Fact]
    public void TableReferences_IgnoreUnqualifiedCteAndIncludeOptimizerRemovedTable()
    {
        var refs = RelationalRewriteRule.TableReferences("WITH x AS (SELECT c.id FROM lab.customers c) SELECT * FROM x LEFT JOIN lab.orders o ON true");
        Assert.Equal(2, refs.Count); Assert.Contains(new DatabaseTableReference("lab", "orders"), refs);
    }

    internal static DatabaseConnectionOptions Options() => new() { Database = "test", Username = "reader", Schema = "lab" };
    internal static CombinedAnalysisResult Original(string sql) => new(new StatementPlanPair(new SqlStatementSummary { StatementIndex = 1 },
        new ExecutionPlan(1, new PlanNode { NodeType = "Seq Scan", RelationName = "customers" })
        { Origin = new PlanOrigin { Source = PlanSourceKind.Database, SqlText = sql, StatementIndex = 1, Host = "localhost", Port = 5432, Database = "test", Schema = "lab", Username = "reader" } }), [], []);

    [Theory]
    [InlineData("missing")]
    [InlineData("nullable")]
    [InlineData("timeout")]
    [InlineData("changed_sql")]
    [InlineData("deferrable")]
    public async Task FreshPreflight_BlocksChangedOrUnknownMetadata(string change)
    {
        string sql = change == "deferrable" ? Distinct : NotIn;
        var suggestion = Assert.Single(Suggest(sql)); var fake = new FakeExplain();
        var metadata = new FakeTables { Items = [Table("customers"), Table("orders")] };
        if (change == "missing") metadata.Items.Clear();
        if (change == "nullable") metadata.Items[1] = Table("orders", true);
        if (change == "deferrable") metadata.Items[0] = Table("customers", deferrable: true);
        if (change == "timeout") metadata.Throw = true;
        if (change == "changed_sql") suggestion = suggestion with { SuggestedSql = "SELECT 1", RequiresRelationalMetadata = false };
        var service = new SqlRewriteComparisonService(fake, new SqlAnalysisService([]), new PlanAnalysisService([]), relationalMetadataService: metadata);
        await Assert.ThrowsAsync<RewriteMetadataException>(() => service.CompareAsync(Original(sql), suggestion, Options(), "", false));
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task FreshPreflight_ValidatesAllTablesBeforeExecuting()
    {
        var fake = new FakeExplain(); var metadata = new FakeTables { Items = [Table("customers"), Table("orders")] };
        var service = new SqlRewriteComparisonService(fake, new SqlAnalysisService([]), new PlanAnalysisService([]), relationalMetadataService: metadata);
        var comparison = await service.CompareAsync(Original(NotIn), Assert.Single(Suggest(NotIn)), Options(), "", false);
        Assert.Equal(MetadataCheckStatus.Passed, comparison.MetadataCheck.Status); Assert.Equal(2, metadata.Calls); Assert.Equal(1, fake.Calls);
    }

    [Theory]
    [InlineData("IsDomain")]
    [InlineData("Generated")]
    [InlineData("IsNullable")]
    public void UnsupportedColumnMetadata_DeclinesNotIn(string field)
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(Table("orders")))!;
        json["Columns"]![1]![field] = field == "Generated" ? System.Text.Json.Nodes.JsonValue.Create("v")
            : field == "IsDomain" ? System.Text.Json.Nodes.JsonValue.Create(true) : null;
        var table = System.Text.Json.JsonSerializer.Deserialize<DatabaseTableMetadata>(json.ToJsonString())!;
        Assert.Empty(Suggest(NotIn, Table("customers"), table));
    }

    [Fact]
    public async Task CancelledPreflight_DoesNotRunExplain()
    {
        var fake = new FakeExplain(); var tables = new FakeTables { Items = [Table("customers"), Table("orders")] };
        var service = new SqlRewriteComparisonService(fake, new SqlAnalysisService([]), new PlanAnalysisService([]), relationalMetadataService: tables);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CompareAsync(Original(NotIn), Assert.Single(Suggest(NotIn)), Options(), "", false, cancellation.Token));
        Assert.Equal(0, tables.Calls); Assert.Equal(0, fake.Calls);
    }

    internal sealed class FakeTables : IDatabaseIndexMetadataService
    {
        public List<DatabaseTableMetadata> Items = [];
        public bool Throw; public int Calls;
        public Task<DatabaseTableMetadata?> GetTableAsync(DatabaseConnectionOptions o, string p, DatabaseTableReference t, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); Calls++; if (Throw) throw new TimeoutException(); return Task.FromResult(Items.SingleOrDefault(i => i.Schema == t.Schema && i.Table == t.Table)); }
    }
    private sealed class FakeExplain : IDatabaseExplainService
    {
        public int Calls;
        public Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions o, string p, string sql, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult("[{\"Plan\":{\"Node Type\":\"Result\"}}]"); }
        public Task<string> GetActualPlanAsync(DatabaseConnectionOptions o, string p, string sql, CancellationToken cancellationToken = default) => GetEstimatedPlanAsync(o, p, sql, cancellationToken);
    }
}
