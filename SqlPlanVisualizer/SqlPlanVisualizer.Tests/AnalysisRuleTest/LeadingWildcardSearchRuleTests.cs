using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class LeadingWildcardSearchRuleTests
    {
        [Fact]
        public void Analyze_MixedOccurrences_ReturnsOnlyLeadingWildcardFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 3,
                StatementType = "SELECT"
            };

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "%adam",
                IsCaseInsensitive = true,
                HasWildcard = true,
                HasLeadingWildcard = true,
                Location = "select_stmt / where_clause",
                SourceOffset = 72
            });

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "eva%",
                HasWildcard = true,
                HasLeadingWildcard = false,
                SourceOffset = 100
            });

            var finding = Assert.Single(new LeadingWildcardSearchRule().Analyze(statement));

            Assert.Equal("LEADING_WILDCARD_SEARCH", finding.RuleId);
            Assert.Equal(3, finding.StatementIndex);
            Assert.Equal((int?)72, finding.SourceOffset);
            Assert.Contains("%adam", finding.Description);
            Assert.Contains("ILIKE", finding.Description);
        }
    }
}