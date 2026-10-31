using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class HighFilterRemovalRuleTests
    {
        [Fact]
        public void Analyze_ReturnsFindingOnlyWhenBothThresholdsAreMet()
        {
            var node = new PlanNode
            {
                NodeType = "Seq Scan",
                ActualRows = 100,
                ActualLoops = 20
            };

            node.AdditionalProperties["Rows Removed by Filter"] = JsonSerializer.SerializeToElement(900);

            var rule = new HighFilterRemovalRule(10000, 0.9);
            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("HIGH_FILTER_REMOVAL", finding.RuleId);
            Assert.Same(node, finding.Node);

            node.ActualLoops = 1;
            Assert.Empty(rule.Analyze(node));

            node.ActualLoops = 20;
            node.ActualRows = 900;
            Assert.Empty(rule.Analyze(node));

            node.NodeType = "Index Scan";
            node.ActualRows = 100;
            node.ActualLoops = 20;

            node.AdditionalProperties["Index Cond"] =
                JsonSerializer.SerializeToElement("(customer_id > 100)");

            node.AdditionalProperties["Filter"] =
                JsonSerializer.SerializeToElement("(total_amount > 1000)");

            var indexFinding = Assert.Single(rule.Analyze(node));

            Assert.Contains("Index Cond: (customer_id > 100)", indexFinding.Description);
            Assert.Contains("Filter: (total_amount > 1000)", indexFinding.Description);
            Assert.Contains("indexes hozzáférés után", indexFinding.Recommendation);
        }
    }
}