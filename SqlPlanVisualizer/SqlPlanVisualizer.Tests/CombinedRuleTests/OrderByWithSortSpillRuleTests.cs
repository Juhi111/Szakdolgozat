using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.CombinedRules;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using System;
using Xunit;

namespace SqlPlanVisualizer.Tests.CombinedRuleTests
{
    public class OrderByWithSortSpillRuleTests
    {
        [Fact]
        public void Analyze_ReturnsFindingOnlyForTopLevelSort()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT",
                TopLevelQuery = new SqlQueryBlockSummary
                {
                    HasOrderBy = true,
                    HasRowLimit = true
                }
            };

            var limit = new PlanNode { NodeType = "Limit" };
            var sort = new PlanNode { NodeType = "Sort" };
            var innerSort = new PlanNode { NodeType = "Sort" };

            limit.Children.Add(sort);
            sort.Children.Add(innerSort);

            var pair = new StatementPlanPair(statement, new ExecutionPlan(1, limit));

            var service = new CombinedAnalysisService(new ICombinedAnalysisRule[]
            {
                new OrderByWithSortSpillRule()
            });

            var topLevelFinding = new PlanAnalysisFinding(sort)
            {
                PlanIndex = 1,
                RuleId = "SORT_SPILL",
                Description = "A rendezés lemezterületet használt.",
                Recommendation = "Vizsgáld meg a rendezés bemenetét."
            };

            var result = service.Analyze(
                pair,
                Array.Empty<SqlAnalysisFinding>(),
                new[] { topLevelFinding });

            var finding = Assert.Single(result.CombinedFindings);

            Assert.Equal("ORDER_BY_WITH_SORT_SPILL", finding.RuleId);
            Assert.Same(topLevelFinding, finding.RelatedPlanFinding);

            var innerFinding = new PlanAnalysisFinding(innerSort)
            {
                PlanIndex = 1,
                RuleId = "SORT_SPILL"
            };

            var innerResult = service.Analyze(
                pair,
                Array.Empty<SqlAnalysisFinding>(),
                new[] { innerFinding });

            Assert.Empty(innerResult.CombinedFindings);
        }
    }
}