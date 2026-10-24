using System;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class NotInSubqueryRuleTests
    {
        [Theory]
        [InlineData(null, "customer_id")]
        [InlineData("c", "c.customer_id")]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding(string? qualifier, string expectedColumnReference)
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.NotInSubqueryOccurrences.Add(
                new NotInSubqueryOccurrence
                {
                    ColumnName = "customer_id",
                    Qualifier = qualifier,
                    Location = "select_stmt / where_clause",
                    SourceOffset = 45
                });

            var findings = new NotInSubqueryRule().Analyze(statement);

            var finding = Assert.Single(findings);

            Assert.Equal("NOT_IN_SUBQUERY", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);

            Assert.Contains(
                expectedColumnReference,
                finding.Description);

            Assert.Contains(
                "select_stmt / where_clause",
                finding.Description);

            Assert.False(string.IsNullOrWhiteSpace(finding.Title));
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
        }

        [Fact]
        public void Analyze_WithoutOccurrences_ReturnsNoFindings()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            var findings = new NotInSubqueryRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_NullStatement_ThrowsArgumentNullException()
        {
            var rule = new NotInSubqueryRule();

            Assert.Throws<ArgumentNullException>(() => rule.Analyze(null!));
        }
    }
}