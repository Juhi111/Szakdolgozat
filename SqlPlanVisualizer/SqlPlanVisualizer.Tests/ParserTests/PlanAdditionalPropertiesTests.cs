using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class PlanAdditionalPropertiesTests
    {
        [Fact]
        public void ParseAndMap_SortProperties_PreservesAdditionalValues()
        {
            string json = """
                [
                  {
                    "Plan": {
                      "Node Type": "Sort",
                      "Sort Space Type": "Disk",
                      "Sort Space Used": 2048
                    }
                  }
                ]
                """;

            var plans = new PostgreSqlPlanParser().Parse(json);
            var plan = Assert.Single(plans);

            var node = new PostgreSqlPlanMapper().MapNode(plan.Plan!);

            Assert.Equal("Sort", node.NodeType);

            Assert.True(PlanNodePropertyReader.TryReadString(node, "Sort Space Type", out var spaceType));
            Assert.Equal("Disk", spaceType);

            Assert.True(PlanNodePropertyReader.TryReadDouble(node, "Sort Space Used", out var spaceUsed));
            Assert.Equal(2048d, spaceUsed);

            Assert.False(PlanNodePropertyReader.TryReadDouble(node, "Sort Space Type", out _));
            Assert.False(PlanNodePropertyReader.TryReadString(node, "Missing Field", out _));
        }
    }
}