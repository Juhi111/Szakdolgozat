using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests
{
    public class PostgreSqlPlanServiceTests
    {
        [Fact]
        public void ParsePlans_WithTiming_PreservesRootAndTimes()
        {
            const string json = """
                [
                  {
                    "Plan": {
                      "Node Type": "Seq Scan",
                      "Relation Name": "orders",
                      "Plan Rows": 100
                    },
                    "Planning Time": 0.5,
                    "Execution Time": 12.3
                  }
                ]
                """;

            var plan = Assert.Single(new PostgreSqlPlanService().ParsePlans(json));

            Assert.Equal(1, plan.PlanIndex);
            Assert.Equal("Seq Scan", plan.Root.NodeType);
            Assert.Equal("orders", plan.Root.RelationName);
            Assert.Equal((double?)0.5, plan.PlanningTime);
            Assert.Equal((double?)12.3, plan.ExecutionTime);
        }
    }
}