using System.Text.Json;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class IndexMetadataTests
{
    private static DatabaseIndexMetadata Index(DatabaseIndexElement[] keys, bool valid = true, string? predicate = null) => new()
    {
        Name = "idx_test", Method = "btree", IsValid = valid, IsReady = true, IsLive = true, Predicate = predicate, Elements = keys.ToList()
    };
    private static DatabaseTableMetadata Table(params DatabaseIndexMetadata[] indexes) => new() { Schema = "lab", Table = "customers", Indexes = indexes.ToList() };
    private static PlanNode Root() => new() { NodeType = "Seq Scan", RelationName = "customers", AdditionalProperties = { ["Schema"] = JsonSerializer.SerializeToElement("lab") } };
    private static string Description(string sql, DatabaseIndexMetadata index) => Assert.Single(IndexMetadataAnalysisService.Evaluate(Table(index), sql, Root())).Description;

    [Theory]
    [InlineData("lower((email)::text)", true)]
    [InlineData("pg_catalog.lower(email)", true)]
    [InlineData("upper(email)", false)]
    [InlineData("lower(other)", false)]
    [InlineData("lower(email::integer)", false)]
    public void FunctionIndex_RecognizesOnlySupportedExpression(string expression, bool matches)
    {
        var text = Description("SELECT * FROM lab.customers c WHERE lower(c.email) = 'a'", Index([new() { Position = 1, Definition = expression }]));
        Assert.Equal(matches, text.Contains("idx_test"));
    }

    [Fact]
    public void InvalidAndIncludedExpressions_AreNotCandidates()
    {
        var key = new DatabaseIndexElement { Position = 1, Definition = "lower(email)" };
        Assert.DoesNotContain("idx_test", Description("SELECT * FROM lab.customers WHERE lower(email)='a'", Index([key], false)));
        Assert.DoesNotContain("idx_test", Description("SELECT * FROM lab.customers ORDER BY email", Index([new() { Column = "email", Included = true }])));
    }

    [Fact]
    public void PartialIndex_ExplicitlyRequiresPredicateCheck()
    {
        var text = Description("SELECT * FROM lab.customers WHERE lower(email)='a'", Index([new() { Definition = "lower(email)" }], predicate: "active"));
        Assert.Contains("idx_test", text);
        Assert.Contains("részleges index", text);
        Assert.Contains("külön ellenőrizni", text);
    }

    [Theory]
    [InlineData("ORDER BY email", true)]
    [InlineData("ORDER BY email DESC", true)]
    [InlineData("ORDER BY email ASC NULLS FIRST", false)]
    [InlineData("ORDER BY email DESC NULLS LAST", false)]
    [InlineData("ORDER BY email, id", false)]
    [InlineData("ORDER BY id", false)]
    public void Sort_MatchesLeadingKeysAndReverseNullOrder(string order, bool matches)
    {
        var text = Description("SELECT * FROM lab.customers " + order, Index([new() { Position = 1, Column = "email" }]));
        Assert.Equal(matches, text.Contains("idx_test"));
    }

    [Fact]
    public void MixedDirectionIndex_MustReverseAllKeysTogether()
    {
        var index = Index([new() { Position = 1, Column = "email" }, new() { Position = 2, Column = "id", Descending = true, NullsFirst = true }]);
        Assert.Contains("idx_test", Description("SELECT * FROM lab.customers ORDER BY email DESC, id ASC", index));
        Assert.DoesNotContain("idx_test", Description("SELECT * FROM lab.customers ORDER BY email DESC, id DESC", index));
    }

    [Fact]
    public void BitmapChild_InheritsOnlyExactRelationAndSchema()
    {
        var root = Root(); root.NodeType = "Bitmap Heap Scan";
        var child = new PlanNode { NodeType = "Bitmap Index Scan", ActualLoops = 0 };
        child.AdditionalProperties["Index Name"] = JsonSerializer.SerializeToElement("idx_test"); root.Children.Add(child);
        var observations = IndexMetadataAnalysisService.Evaluate(Table(Index([])), "SELECT * FROM lab.customers", root);
        Assert.Contains(observations, o => o.Title == "A tervben szereplő index" && o.Description.Contains("nem bizonyítja"));
        root.AdditionalProperties["Schema"] = JsonSerializer.SerializeToElement("elsewhere");
        Assert.DoesNotContain(IndexMetadataAnalysisService.Evaluate(Table(Index([])), "SELECT * FROM lab.customers", root), o => o.Title == "A tervben szereplő index");
    }

    [Fact]
    public void SubqueryAndJoin_DoNotReceiveStructuralMatches()
    {
        foreach (var sql in new[] { "SELECT * FROM lab.customers WHERE EXISTS (SELECT 1 WHERE lower(email)='a')", "SELECT * FROM lab.customers c JOIN lab.orders o ON true WHERE lower(c.email)='a'" })
            Assert.Contains(IndexMetadataAnalysisService.Evaluate(Table(Index([new() { Definition = "lower(email)" }])), sql, Root()), o => o.Title == "Illeszkedésvizsgálat");
    }

    [Fact]
    public void QuotedIdentifierCase_IsPreserved()
    {
        var table = Table(Index([new() { Definition = "lower(\"Email\")" }]));
        Assert.Contains(IndexMetadataAnalysisService.Evaluate(table, "SELECT * FROM lab.customers WHERE lower(\"Email\")='a'", Root()), o => o.Description.Contains("idx_test"));
        Assert.DoesNotContain(IndexMetadataAnalysisService.Evaluate(table, "SELECT * FROM lab.customers WHERE lower(email)='a'", Root()), o => o.Description.Contains("idx_test"));
    }

    private sealed class FakeService : IDatabaseIndexMetadataService
    {
        public int Calls;
        public Task<DatabaseTableMetadata?> GetTableAsync(DatabaseConnectionOptions options, string password, DatabaseTableReference table, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult<DatabaseTableMetadata?>(null); }
    }
    private static CombinedAnalysisResult Result(PlanSourceKind kind = PlanSourceKind.Database) => new(new(new() { StatementIndex = 1 }, new(1, Root())
    { Origin = new() { Source = kind, Host = "localhost", Port = 5432, Database = "db", Schema = "public", Username = "u", StatementIndex = 1, SqlText = "SELECT * FROM lab.customers" } }), [], []);
    private static DatabaseConnectionOptions Options() => new() { Database = "db", Username = "u" };

    [Fact]
    public async Task FilePlanAndDifferentConnection_AreRejectedBeforeDatabaseAccess()
    {
        var fake = new FakeService(); var service = new IndexMetadataAnalysisService(fake);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadAsync(Result(PlanSourceKind.JsonFile), Options(), ""));
        var options = Options(); options.Database = "other";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadAsync(Result(), options, ""));
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task MissingMetadata_IsNotReportedAsNoIndexes()
    {
        var report = await new IndexMetadataAnalysisService(new FakeService()).LoadAsync(Result(), Options(), "");
        Assert.Empty(report.Tables); Assert.Contains("nem érhető el", Assert.Single(report.Notices));
    }

    [Fact]
    public async Task Cancellation_PreventsDatabaseAccess()
    {
        var fake = new FakeService();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new IndexMetadataAnalysisService(fake).LoadAsync(Result(), Options(), "", new CancellationToken(true)));
        Assert.Equal(0, fake.Calls);
    }
}
