using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class IntermediateStorageSpillRuleTests
    {
        [Fact]
        public void Analyze_Storage_ReturnsFindingOnlyForDisk()
        {
            var node = new PlanNode
            {
                NodeType = "Materialize",
                ActualRows = 50000,
                ActualLoops = 1
            };

            node.AdditionalProperties["Storage"] = JsonSerializer.SerializeToElement("Disk");
            node.AdditionalProperties["Maximum Storage"] = JsonSerializer.SerializeToElement(5736);

            var rule = new IntermediateStorageSpillRule();
            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("INTERMEDIATE_STORAGE_SPILL", finding.RuleId);
            Assert.Same(node, finding.Node);
            Assert.Contains("Materialize", finding.Description);

            node.AdditionalProperties["Storage"] = JsonSerializer.SerializeToElement("Memory");

            Assert.Empty(rule.Analyze(node));

            node.AdditionalProperties.Clear();

            Assert.Empty(rule.Analyze(node));
        }
    }
}