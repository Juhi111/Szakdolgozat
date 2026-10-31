using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using System.Text.Json;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class RepeatedSequentialScanRuleTests
    {
        [Fact]
        public void Analyze_DetectsRepeatedScanInInnerBranch()
        {
            var join = new PlanNode { NodeType = "Nested Loop" };
            var aggregate = new PlanNode { NodeType = "Aggregate" };

            aggregate.AdditionalProperties["Parent Relationship"] =
                JsonSerializer.SerializeToElement("Inner");

            var scan = new PlanNode
            {
                NodeType = "Seq Scan",
                ActualRows = 10,
                ActualLoops = 200
            };

            scan.AdditionalProperties["Parent Relationship"] =
                JsonSerializer.SerializeToElement("Outer");

            scan.AdditionalProperties["Rows Removed by Filter"] =
                JsonSerializer.SerializeToElement(149990);

            join.Children.Add(aggregate);
            aggregate.Children.Add(scan);

            var rule = new RepeatedSequentialScanRule(100, 1000000);
            var service = new PlanAnalysisService(new IPlanAnalysisRule[] { rule });

            var finding = Assert.Single(service.Analyze(new[] { join }));

            Assert.Equal("REPEATED_SEQUENTIAL_SCAN", finding.RuleId);
            Assert.Same(scan, finding.Node);

            scan.ActualLoops = 1;

            Assert.Empty(service.Analyze(new[] { join }));
        }
    }
}