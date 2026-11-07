using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.CombinedRules;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.CombinedRuleTests
{
    public class OrderedLimitWithLargeSortRuleTests
    {
        [Fact]
        public void Analyze_OrderedLimit_MatchesOnlyRootFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT",
                TopLevelQuery = new SqlQueryBlockSummary
                {
                    HasOrderBy = true,
                    HasRowLimit = true
                }
            };

            var root = new PlanNode { NodeType = "Limit" };
            var innerLimit = new PlanNode { NodeType = "Limit" };
            root.Children.Add(innerLimit);

            var pair = new StatementPlanPair(statement, new ExecutionPlan(1, root));
            var service = new CombinedAnalysisService(new ICombinedAnalysisRule[]
            {
                new OrderedLimitWithLargeSortRule()
            });

            var rootFinding = new PlanAnalysisFinding(root)
            {
                PlanIndex = 1,
                RuleId = "LARGE_SORT_FOR_LIMIT"
            };

            var result = service.Analyze(
                pair, Array.Empty<SqlAnalysisFinding>(), new[] { rootFinding });

            var combined = Assert.Single(result.CombinedFindings);

            Assert.Equal("ORDERED_LIMIT_WITH_LARGE_SORT", combined.RuleId);
            Assert.Same(rootFinding, combined.RelatedPlanFinding);

            var innerFinding = new PlanAnalysisFinding(innerLimit)
            {
                PlanIndex = 1,
                RuleId = "LARGE_SORT_FOR_LIMIT"
            };

            var innerResult = service.Analyze(
                pair, Array.Empty<SqlAnalysisFinding>(), new[] { innerFinding });

            Assert.Empty(innerResult.CombinedFindings);
        }
    }
}