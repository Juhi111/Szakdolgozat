using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class NaturalJoinUsageRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.NaturalJoins.Add(new NaturalJoinOccurrence
            {
                Location = "select_stmt / from_clause[0] / join_expr",
                SourceOffset = 67
            });

            var findings = new NaturalJoinUsageRule().Analyze(statement);
            var finding = Assert.Single(findings);

            Assert.Equal("NATURAL_JOIN_USAGE", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)67, finding.SourceOffset);
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
        }
    }
}