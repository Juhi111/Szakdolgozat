using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using System;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class LeadingWildcardSearchRuleTests
    {
        [Theory]
        [InlineData("%adam%", false)]
        [InlineData("_adam", false)]
        [InlineData("%adam%", true)]
        [InlineData("_adam", true)]
        public void Analyze_WithLeadingWildcard_ReturnsFinding(string pattern, bool isCaseInsensitive)
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 3,
                StatementType = "SELECT"
            };

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = pattern,
                IsCaseInsensitive = isCaseInsensitive,
                HasWildcard = true,
                HasLeadingWildcard = true,
                Location = "select_stmt / where_clause",
                SourceOffset = 72
            });

            var findings = new LeadingWildcardSearchRule().Analyze(statement);

            var finding = Assert.Single(findings);

            Assert.Equal("LEADING_WILDCARD_SEARCH", finding.RuleId);
            Assert.Equal(3, finding.StatementIndex);
            Assert.Equal((int?)72, finding.SourceOffset);

            Assert.Contains(pattern, finding.Description);
            Assert.Contains("select_stmt / where_clause", finding.Description);

            string expectedOperator = isCaseInsensitive
                ? "ILIKE"
                : "LIKE";

            Assert.Contains($"A {expectedOperator} mintája", finding.Description);

            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
        }

        [Theory]
        [InlineData("adam", false)]
        [InlineData("adam%", true)]
        [InlineData("ad_m", true)]
        [InlineData("!%adam", false)]
        [InlineData("!!%", true)]
        public void Analyze_WithoutLeadingWildcard_ReturnsNoFindings(
            string pattern,
            bool hasWildcard)
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = pattern,
                HasWildcard = hasWildcard,
                HasLeadingWildcard = false
            });

            var findings = new LeadingWildcardSearchRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_MixedOccurrences_ReturnsOnlyMatchingFindings()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "%adam",
                HasWildcard = true,
                HasLeadingWildcard = true,
                SourceOffset = 40
            });

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "eva%",
                HasWildcard = true,
                HasLeadingWildcard = false,
                SourceOffset = 70
            });

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "_peter",
                HasWildcard = true,
                HasLeadingWildcard = true,
                SourceOffset = 100
            });

            var findings = new LeadingWildcardSearchRule().Analyze(statement);

            Assert.Collection(
                findings,
                first =>
                {
                    Assert.Equal(2, first.StatementIndex);
                    Assert.Equal((int?)40, first.SourceOffset);
                },
                second =>
                {
                    Assert.Equal(2, second.StatementIndex);
                    Assert.Equal((int?)100, second.SourceOffset);
                });
        }

        [Fact]
        public void Analyze_MissingSourceOffset_PreservesNull()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            statement.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = "%adam",
                HasWildcard = true,
                HasLeadingWildcard = true,
                SourceOffset = null
            });

            var findings = new LeadingWildcardSearchRule().Analyze(statement);

            var finding = Assert.Single(findings);

            Assert.Null(finding.SourceOffset);
        }

        [Fact]
        public void Analyze_WithoutOccurrences_ReturnsNoFindings()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            var findings = new LeadingWildcardSearchRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_NullStatement_ThrowsArgumentNullException()
        {
            var rule = new LeadingWildcardSearchRule();

            Assert.Throws<ArgumentNullException>(() => rule.Analyze(null!));
        }
    }
}