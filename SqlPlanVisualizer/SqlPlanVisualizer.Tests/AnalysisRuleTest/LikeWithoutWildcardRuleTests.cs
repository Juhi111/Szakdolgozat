using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class LikeWithoutWildcardRuleTests
    {
        [Fact]
        public void Analyze_MixedOccurrences_ReturnsOnlyPatternWithoutWildcardFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "adam",
                HasWildcard = false,
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "eva%",
                HasWildcard = true,
                SourceOffset = 80
            });

            var finding = Assert.Single(new LikeWithoutWildcardRule().Analyze(statement));

            Assert.Equal("LIKE_WITHOUT_WILDCARD", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("adam", finding.Description);
        }
    }
}