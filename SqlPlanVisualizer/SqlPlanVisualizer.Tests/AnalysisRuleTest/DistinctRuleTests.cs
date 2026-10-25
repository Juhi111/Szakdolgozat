using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.Rules;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class DistinctRuleTests
    {
        [Fact]
        public void Analyze_WithMixedOccurrences_ReturnsOnlyPlainDistinctFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.DistinctOccurrences.Add(new DistinctOccurrence
            {
                Location = "outer",
                IsDistinctOn = true
            });

            statement.DistinctOccurrences.Add(new DistinctOccurrence
            {
                Location = "inner",
                IsDistinctOn = false,
                HasJoin = true
            });

            var finding = Assert.Single(new DistinctRule().Analyze(statement));

            Assert.Equal("DISTINCT_USAGE", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Contains("inner", finding.Description);
            Assert.Contains("JOIN", finding.Recommendation);
        }
    }
}