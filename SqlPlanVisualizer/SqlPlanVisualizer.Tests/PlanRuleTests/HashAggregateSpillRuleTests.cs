using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class HashAggregateSpillRuleTests
    {
        [Fact]
        public void Analyze_ReturnsFindingOnlyForPositiveDiskUsage()
        {
            var node = new PlanNode
            {
                NodeType = "Aggregate"
            };

            node.AdditionalProperties["Strategy"] = JsonSerializer.SerializeToElement("Hashed");
            node.AdditionalProperties["Disk Usage"] = JsonSerializer.SerializeToElement(4096);

            var rule = new HashAggregateSpillRule();
            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("HASH_AGGREGATE_SPILL", finding.RuleId);
            Assert.Same(node, finding.Node);

            node.AdditionalProperties["Disk Usage"] = JsonSerializer.SerializeToElement(0);

            Assert.Empty(rule.Analyze(node));

            node.AdditionalProperties["Workers"] = JsonSerializer.SerializeToElement(new[]
            {
                new Dictionary<string, object>
                {
                    ["Worker Number"] = 0,
                    ["Disk Usage"] = 4096
                }
            });

            var workerFinding = Assert.Single(rule.Analyze(node));

            Assert.Contains("Worker 0", workerFinding.Description);
            Assert.Same(node, workerFinding.Node);
        }
    }
}