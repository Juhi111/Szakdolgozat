using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class SortSpillRuleTests
    {
        [Fact]
        public void Analyze_DiskSort_ReturnsFindingButMemorySortDoesNot()
        {
            var node = new PlanNode
            {
                NodeType = "Sort"
            };

            node.AdditionalProperties["Sort Space Type"] = JsonSerializer.SerializeToElement("Disk");
            node.AdditionalProperties["Sort Space Used"] = JsonSerializer.SerializeToElement(2048);

            var rule = new SortSpillRule();
            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("SORT_SPILL", finding.RuleId);
            Assert.Same(node, finding.Node);

            node.AdditionalProperties["Sort Space Type"] = JsonSerializer.SerializeToElement("Memory");

            Assert.Empty(rule.Analyze(node));

            node.AdditionalProperties["Workers"] = JsonSerializer.SerializeToElement(new[]
            {
                new Dictionary<string, object>
                {
                    ["Worker Number"] = 0,
                    ["Sort Space Type"] = "Disk",
                    ["Sort Space Used"] = 1024
                },
                new Dictionary<string, object>
                {
                    ["Worker Number"] = 1,
                    ["Sort Space Type"] = "Disk",
                    ["Sort Space Used"] = 2048
                }
            });

            var workerFinding = Assert.Single(rule.Analyze(node));

            Assert.Contains("Worker 0", workerFinding.Description);
            Assert.Contains("Worker 1", workerFinding.Description);
            Assert.Same(node, workerFinding.Node);
        }
    }
}