using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.History;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class AnalysisOverviewTests
{
    [Fact]
    public void SameRuleOnThreeNodes_IsOneCardWithAllMetricsAndPaths()
    {
        var root = new PlanNode { NodeType = "Sort" };
        root.Children.Add(new() { NodeType = "Seq Scan", RelationName = "a" });
        root.Children.Add(new() { NodeType = "Seq Scan", RelationName = "b" });
        var findings = new[] { root, root.Children[0], root.Children[1] }.Select((node, i) => new PlanAnalysisFinding(node)
        {
            RuleId = "ROW_ESTIMATE_MISMATCH", Title = "Becslés", Description = $"Egyedi mért érték: {i}", Recommendation = "Ellenőrzés"
        }).ToArray();
        var result = Result(root, [], findings);
        var entry = new AnalysisHistoryMapper().Map(result, false, null, null);
        // Az új azonosítók előzménymentésen keresztül is megmaradnak.
        entry = JsonSerializer.Deserialize<AnalysisHistoryEntry>(JsonSerializer.Serialize(entry))!;
        var card = Assert.Single(AnalysisOverview.Create(entry));
        Assert.Equal(3, card.Evidence.Count);
        Assert.Contains(card.Evidence, e => e.Label.Contains("1.2") && e.Description.Contains("2"));
    }

    [Fact]
    public void SqlFinding_IsEnrichedAndOnlyItsExactPlanEvidenceIsCovered()
    {
        var root = new PlanNode { NodeType = "Seq Scan", RelationName = "a" };
        var other = new PlanNode { NodeType = "Seq Scan", RelationName = "b" };
        root.Children.Add(other);
        var plan = new PlanAnalysisFinding(root) { RuleId = "HIGH_FILTER_REMOVAL", Title = "Szűrés", Description = "azonos szöveg" };
        var unrelated = new PlanAnalysisFinding(other) { RuleId = plan.RuleId, Title = plan.Title, Description = plan.Description };
        var result = Result(root, [new() { RuleId = "FUNCTION_ON_COLUMN", Title = "Függvény", Description = "lower(a.email)" }], [plan, unrelated]);
        result.CombinedFindings.Add(new()
        {
            RuleId = "FUNCTION_FILTER_WITH_HIGH_REMOVAL", RelatedSqlRuleId = "FUNCTION_ON_COLUMN", Title = "Kapcsolat",
            SqlObservation = "lower(a.email)", PlanObservation = "azonos szöveg", Description = "A teljes szűrésre vonatkozik.",
            Recommendation = "Mérd meg.", RelatedPlanFinding = plan
        });
        var cards = AnalysisOverview.Create(new AnalysisHistoryMapper().Map(result, false, null, null));
        Assert.Equal(2, cards.Count);
        Assert.Equal(2, cards[0].Evidence.Count);
        Assert.Contains(cards[0].Evidence, e => e.Description.Contains("A teljes szűrésre vonatkozik."));
        Assert.Contains("b", Assert.Single(cards[1].Evidence).Label);
    }

    [Fact]
    public void LegacyAmbiguousEvidence_IsNotSilentlyRemoved()
    {
        var entry = new AnalysisHistoryEntry
        {
            PlanFindings = [new() { RuleId = "SORT_SPILL", Description = "same" }, new() { RuleId = "SORT_SPILL", Description = "same" }],
            CombinedFindings = [new() { RuleId = "ORDER_BY_WITH_SORT_SPILL", PlanObservation = "same" }]
        };
        var cards = AnalysisOverview.Create(entry);
        Assert.Equal(2, cards.Count);
        Assert.Equal(2, cards[1].Evidence.Count);
    }

    [Fact]
    public void DuplicateIdentity_IsCollapsedButDifferentMetricsArePreserved()
    {
        var entry = new AnalysisHistoryEntry { PlanFindings =
        [new() { RuleId = "X", EvidenceKey = "X:1", Description = "a" },
         new() { RuleId = "X", EvidenceKey = "X:1", Description = "a" },
         new() { RuleId = "X", EvidenceKey = "X:1", Description = "b" }] };
        Assert.Equal(2, Assert.Single(AnalysisOverview.Create(entry)).Evidence.Count);
    }

    [Theory]
    [InlineData("LEFT_JOIN_ANTI_REWRITE", false)]
    [InlineData("LIKE_LITERAL_REWRITE", true)]
    public void RewriteDetails_AreOnlyShownWhenTheyAddInformation(string rule, bool expected)
    {
        var suggestion = new SqlRewriteSuggestion { RuleId = rule, Title = "", OriginalSql = "", SuggestedSql = "", Explanation = "Részletes magyarázat", Preconditions = "" };
        Assert.Equal(expected, suggestion.HasDetailedExplanation);
    }

    private static CombinedAnalysisResult Result(PlanNode root, IEnumerable<SqlAnalysisFinding> sql, IEnumerable<PlanAnalysisFinding> plan) =>
        new(new(new() { StatementIndex = 1 }, new(1, root)), sql, plan);
}
