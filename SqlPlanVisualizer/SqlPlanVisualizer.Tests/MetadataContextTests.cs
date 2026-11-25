using System.Text.Json;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class MetadataContextTests
{
    private static PlanNode Node(string table, string? schema = "lab")
    {
        var node = new PlanNode { NodeType = "Seq Scan", RelationName = table };
        if (schema is not null) node.AdditionalProperties["Schema"] = JsonSerializer.SerializeToElement(schema);
        return node;
    }
    private static CombinedAnalysisResult Result(PlanNode node, string rule) => new(new StatementPlanPair(new SqlStatementSummary { StatementIndex = 1 }, new ExecutionPlan(1, node)), [], [new(node) { RuleId = rule }]);

    [Theory]
    [InlineData("HIGH_FILTER_REMOVAL")]
    [InlineData("ROW_ESTIMATE_MISMATCH")]
    [InlineData("HIGH_HEAP_FETCHES")]
    [InlineData("REPEATED_SEQUENTIAL_SCAN")]
    public void TableEvidence_IsNotAssignedToAnotherRelation(string rule)
    {
        var table = RelationalRewriteTests.Table("customers");
        Assert.NotEmpty(MetadataContextEvaluator.Evaluate(table, Result(Node("customers"), rule)));
        Assert.Empty(MetadataContextEvaluator.Evaluate(table, Result(Node("orders"), rule)));
        Assert.Empty(MetadataContextEvaluator.Evaluate(table, Result(Node("customers", "other"), rule)));
        Assert.Empty(MetadataContextEvaluator.Evaluate(table, Result(Node("customers", null), rule)));
        Assert.Empty(MetadataContextEvaluator.Evaluate(table, Result(new PlanNode { NodeType = "Sort" }, rule)));
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(0, 0, false)]
    [InlineData(10, 11, false)]
    [InlineData(null, null, false)]
    public void Visibility_HandlesMissingAndInconsistentStatistics(int? pages, int? visible, bool ratio)
    {
        var table = new DatabaseTableMetadata { Schema = "lab", Table = "customers", Statistics = new() { Pages = pages, AllVisiblePages = visible } };
        var text = Assert.Single(MetadataContextEvaluator.Evaluate(table, Result(Node("customers"), "HIGH_HEAP_FETCHES"))).Description;
        Assert.Equal(ratio, text.Contains("%")); Assert.DoesNotContain("NaN", text);
    }

    [Fact]
    public async Task MetadataWithoutRewrite_IsCollectedForTableNotInPlanAndSavedInAssessment()
    {
        const string sql = "SELECT c.name FROM lab.customers c LEFT JOIN lab.orders o ON c.id=o.customer_id";
        var result = RelationalRewriteTests.Original(sql);
        var service = new RelationalRewriteTests.FakeTables { Items = [RelationalRewriteTests.Table("customers"), RelationalRewriteTests.Table("orders", valid: false)] };
        var report = await new IndexMetadataAnalysisService(service).LoadAsync(result, RelationalRewriteTests.Options(), "");
        Assert.Equal(2, report.Tables.Count);
        Assert.Contains(report.Tables.Single(t => t.Metadata.Table == "orders").Observations, o => o.Title == "Nem használható állapotú index");
        result.SetDatabaseMetadata(report);
        Assert.Contains(result.IndexAssessment!.Items, i => i.Title == "Nem használható állapotú index");
    }

    [Fact]
    public void PredicateEvidence_ReportsTypeAndNullableStatisticsWithoutRewrite()
    {
        const string sql = "SELECT c.name FROM lab.customers c WHERE lower(c.name) = 'a'";
        var original = RelationalRewriteTests.Original(sql);
        var result = new CombinedAnalysisResult(original.Pair, [new() { RuleId = "FUNCTION_ON_COLUMN" }], []);
        var observations = MetadataContextEvaluator.Evaluate(RelationalRewriteTests.Table("customers"), result).ToList();
        Assert.Contains(observations, o => o.Title == "A szűrés oszlopa: name" && o.Description.Contains("megengedett"));
        Assert.Empty(RelationalRewriteTests.Suggest(sql));
    }
}
