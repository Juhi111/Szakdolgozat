using System;
using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.CombinedRules;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.CombinedRuleTests
{
    public class FunctionFilterWithHighRemovalRuleTests
    {
        [Fact]
        public void Analyze_ReturnsFindingOnlyForMatchingRelation()
        {
            var query = new SqlQueryBlockSummary
            {
                HasOnlyDirectRelations = true
            };

            query.Relations.Add(new SqlRelationSummary
            {
                RelationName = "customers",
                Alias = "c"
            });

            query.FunctionsInWhere.Add(new FunctionOnColumnOccurrence
            {
                FunctionName = "lower",
                ColumnName = "email",
                Qualifier = "c"
            });

            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT",
                TopLevelQuery = query
            };

            var node = new PlanNode
            {
                NodeType = "Seq Scan",
                RelationName = "customers"
            };

            node.AdditionalProperties["Alias"] = JsonSerializer.SerializeToElement("c");
            node.AdditionalProperties["Filter"] =
                JsonSerializer.SerializeToElement("(lower(email) = 'adam@example.com'::text)");

            var planFinding = new PlanAnalysisFinding(node)
            {
                PlanIndex = 1,
                RuleId = "HIGH_FILTER_REMOVAL",
                Description = "A művelet sok sort szűrt ki."
            };

            var pair = new StatementPlanPair(statement, new ExecutionPlan(1, node));
            var service = new CombinedAnalysisService(new ICombinedAnalysisRule[]
            {
                new FunctionFilterWithHighRemovalRule()
            });

            var result = service.Analyze(
                pair,
                Array.Empty<SqlAnalysisFinding>(),
                new[] { planFinding });

            var finding = Assert.Single(result.CombinedFindings);

            Assert.Equal("FUNCTION_FILTER_WITH_HIGH_REMOVAL", finding.RuleId);
            Assert.Same(planFinding, finding.RelatedPlanFinding);

            node.AdditionalProperties["Alias"] = JsonSerializer.SerializeToElement("other");

            var unmatchedResult = service.Analyze(
                pair,
                Array.Empty<SqlAnalysisFinding>(),
                new[] { planFinding });

            Assert.Empty(unmatchedResult.CombinedFindings);
        }
    }
}