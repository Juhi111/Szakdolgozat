using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class RandomOrderingRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.RandomOrderings.Add(new RandomOrderingOccurrence
            {
                Location = "select_stmt",
                SourceOffset = 57
            });

            var findings = new RandomOrderingRule().Analyze(statement);
            var finding = Assert.Single(findings);

            Assert.Equal("RANDOM_ORDERING", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)57, finding.SourceOffset);
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
        }
    }
}