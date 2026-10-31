using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class HighHeapFetchesRuleTests
    {
        [Fact]
        public void Analyze_UsesHeapFetchesWithoutMultiplyingByLoops()
        {
            var node = new PlanNode
            {
                NodeType = "Index Only Scan",
                ActualRows = 500,
                ActualLoops = 20
            };

            var rule = new HighHeapFetchesRule(10000);

            node.AdditionalProperties["Heap Fetches"] = JsonSerializer.SerializeToElement(9999);

            Assert.Empty(rule.Analyze(node));

            node.AdditionalProperties["Heap Fetches"] = JsonSerializer.SerializeToElement(10000);

            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("HIGH_HEAP_FETCHES", finding.RuleId);
            Assert.Same(node, finding.Node);
        }
    }
}