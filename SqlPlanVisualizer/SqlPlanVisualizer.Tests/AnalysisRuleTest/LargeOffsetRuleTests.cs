using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class LargeOffsetRuleTests
    {
        [Fact]
        public void Analyze_Offsets_ReturnsOnlyFindingAtThreshold()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.OffsetOccurrences.Add(new OffsetOccurrence
            {
                RowOffset = 499,
                SourceOffset = 20
            });

            statement.OffsetOccurrences.Add(new OffsetOccurrence
            {
                RowOffset = 500,
                Location = "select_stmt",
                SourceOffset = 65
            });

            var findings = new LargeOffsetRule(threshold: 500).Analyze(statement);
            var finding = Assert.Single(findings);

            Assert.Equal("LARGE_OFFSET", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)65, finding.SourceOffset);
        }
    }
}