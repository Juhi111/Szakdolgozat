using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class UnionDeduplicationRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.UnionDeduplications.Add(new UnionDeduplicationOccurrence
            {
                Location = "select_stmt",
                SourceOffset = 45
            });

            var findings = new UnionDeduplicationRule().Analyze(statement);
            var finding = Assert.Single(findings);

            Assert.Equal("UNION_DEDUPLICATION", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
        }
    }
}