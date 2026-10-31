using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class HashBatchRuleTests
    {
        [Fact]
        public void Analyze_ReturnsFindingOnlyForMultipleBatches()
        {
            var node = new PlanNode
            {
                NodeType = "Hash"
            };

            node.AdditionalProperties["Hash Batches"] = JsonSerializer.SerializeToElement(8);

            var rule = new HashBatchRule();
            var finding = Assert.Single(rule.Analyze(node));

            Assert.Equal("HASH_MULTIPLE_BATCHES", finding.RuleId);
            Assert.Same(node, finding.Node);

            node.AdditionalProperties["Hash Batches"] = JsonSerializer.SerializeToElement(1);

            Assert.Empty(rule.Analyze(node));
        }
    }
}