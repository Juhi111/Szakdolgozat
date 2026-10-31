using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class LossyBitmapHeapScanRuleTests
    {
        [Fact]
        public void Analyze_DetectsLossyBlocksOnNodeOrWorker()
        {
            var node = new PlanNode
            {
                NodeType = "Bitmap Heap Scan"
            };

            node.AdditionalProperties["Lossy Heap Blocks"] = JsonSerializer.SerializeToElement(200);
            node.AdditionalProperties["Exact Heap Blocks"] = JsonSerializer.SerializeToElement(50);

            var rule = new LossyBitmapHeapScanRule();
            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("LOSSY_BITMAP_HEAP_SCAN", finding.RuleId);
            Assert.Same(node, finding.Node);

            node.AdditionalProperties["Lossy Heap Blocks"] = JsonSerializer.SerializeToElement(0);

            Assert.Empty(rule.Analyze(node));

            node.AdditionalProperties["Workers"] = JsonSerializer.SerializeToElement(new[]
            {
                new Dictionary<string, object>
                {
                    ["Worker Number"] = 0,
                    ["Lossy Heap Blocks"] = 100,
                    ["Exact Heap Blocks"] = 25
                }
            });

            var workerFinding = Assert.Single(rule.Analyze(node));

            Assert.Contains("Worker 0", workerFinding.Description);
            Assert.Same(node, workerFinding.Node);
        }
    }
}