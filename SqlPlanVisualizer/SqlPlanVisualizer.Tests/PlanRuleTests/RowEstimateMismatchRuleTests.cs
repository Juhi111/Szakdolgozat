using PgSqlParser;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using System.Text.Json;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class RowEstimateMismatchRuleTests
    {
        [Fact]
        public void Analyze_LargeMismatch_ReturnsFindingForNode()
        {
            var node = new PlanNode
            {
                NodeType = "Seq Scan",
                PlanRows = 100,
                ActualRows = 10000,
                ActualLoops = 1
            };

            var rule = new RowEstimateMismatchRule(ratioThreshold: 10, differenceThreshold: 1000);

            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("ROW_ESTIMATE_MISMATCH", finding.RuleId);
            Assert.Same(node, finding.Node);
            Assert.Contains("Seq Scan", finding.Description);

            var limitedScan = new PlanNode
            {
                NodeType = "Seq Scan",
                PlanRows = 100000,
                ActualRows = 10,
                ActualLoops = 1
            };

            limitedScan.AdditionalProperties["Parent Relationship"] =
                JsonSerializer.SerializeToElement("Outer");

            var limit = new PlanNode
            {
                NodeType = "Limit",
                ActualRows = 10,
                ActualLoops = 1
            };

            var limitContext = new PlanAnalysisContext(limit, 1);
            var scanContext = new PlanAnalysisContext(limitedScan, 1, limitContext);

            var contextRule = new RowEstimateMismatchRule(10, 1000);
            var limitedFinding = Assert.Single(contextRule.Analyze(limitedScan, scanContext));

            Assert.Contains("korábban leállíthatta", limitedFinding.Description);

            var sort = new PlanNode
            {
                NodeType = "Sort",
                ActualLoops = 1
            };

            sort.AdditionalProperties["Parent Relationship"] =
                JsonSerializer.SerializeToElement("Outer");

            var sortContext = new PlanAnalysisContext(sort, 1, limitContext);
            var scanBelowSortContext = new PlanAnalysisContext(limitedScan, 1, sortContext);

            var sortedFinding = Assert.Single(contextRule.Analyze(limitedScan, scanBelowSortContext));

            Assert.DoesNotContain("korábban leállíthatta", sortedFinding.Description);
        }
    }
}