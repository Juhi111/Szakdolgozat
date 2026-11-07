using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests
{
    public class CombinedAnalysisServiceTests
    {
        [Fact]
        public void Analyze_ReturnsOnlyFindingsForSelectedPair()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            var root = new PlanNode { NodeType = "Sort" };
            var plan = new ExecutionPlan(1, root)
            {
                ExecutionTime = 25
            };

            var sqlFinding = new SqlAnalysisFinding { StatementIndex = 2 };
            var planFinding = new PlanAnalysisFinding(root) { PlanIndex = 1 };

            var result = new CombinedAnalysisService(Array.Empty<ICombinedAnalysisRule>()).Analyze(
                new StatementPlanPair(statement, plan),
                new[]
                {
                    new SqlAnalysisFinding { StatementIndex = 1 },
                    sqlFinding
                },
                new[]
                {
                    planFinding,
                    new PlanAnalysisFinding(root) { PlanIndex = 2 }
                });

            Assert.Same(sqlFinding, Assert.Single(result.SqlFindings));
            Assert.Same(planFinding, Assert.Single(result.PlanFindings));
            Assert.Equal((double?)25, result.ExecutionTime);
        }
    }
}