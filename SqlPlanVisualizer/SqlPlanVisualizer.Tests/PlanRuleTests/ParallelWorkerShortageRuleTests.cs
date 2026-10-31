using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class ParallelWorkerShortageRuleTests
    {
        [Fact]
        public void Analyze_ReturnsFindingOnlyForExecutedNodeWithWorkerShortage()
        {
            var node = new PlanNode
            {
                NodeType = "Gather",
                ActualLoops = 1
            };

            node.AdditionalProperties["Workers Planned"] = JsonSerializer.SerializeToElement(4);
            node.AdditionalProperties["Workers Launched"] = JsonSerializer.SerializeToElement(2);

            var rule = new ParallelWorkerShortageRule();
            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("PARALLEL_WORKER_SHORTAGE", finding.RuleId);
            Assert.Same(node, finding.Node);

            node.ActualLoops = 0;
            Assert.Empty(rule.Analyze(node));

            node.ActualLoops = 1;
            node.AdditionalProperties["Workers Launched"] = JsonSerializer.SerializeToElement(4);

            Assert.Empty(rule.Analyze(node));
        }
    }
}