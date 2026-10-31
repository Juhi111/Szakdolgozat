using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class LargeSortForLimitRuleTests
    {
        [Fact]
        public void Analyze_UsesSortInputInsteadOfSortOutput()
        {
            var limit = new PlanNode
            {
                NodeType = "Limit",
                ActualRows = 100,
                ActualLoops = 1
            };

            var sort = new PlanNode
            {
                NodeType = "Sort",
                ActualRows = 100,
                ActualLoops = 1
            };

            var scan = new PlanNode
            {
                NodeType = "Seq Scan",
                ActualRows = 250000,
                ActualLoops = 1
            };

            sort.AdditionalProperties["Parent Relationship"] =
                JsonSerializer.SerializeToElement("Outer");

            scan.AdditionalProperties["Parent Relationship"] =
                JsonSerializer.SerializeToElement("Outer");

            limit.Children.Add(sort);
            sort.Children.Add(scan);

            var rule = new LargeSortForLimitRule(100000, 100);
            var finding = Assert.Single(rule.Analyze(limit));

            Assert.Equal("LARGE_SORT_FOR_LIMIT", finding.RuleId);
            Assert.Same(limit, finding.Node);

            scan.ActualRows = 1000;

            Assert.Empty(rule.Analyze(limit));
        }
    }
}