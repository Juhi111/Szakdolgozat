using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class ArithmeticOnColumnRuleTests
    {
        [Theory]
        [InlineData(true, null, "total_amount / 2")]
        [InlineData(false, null, "2 / total_amount")]
        [InlineData(true, "o", "o.total_amount / 2")]
        [InlineData(false, "o", "2 / o.total_amount")]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding(bool isColumnOnLeft, string? qualifier, string expectedExpression)
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.ArithmeticOnColumnOccurrences.Add(
                new ArithmeticOnColumnOccurrence
                {
                    ColumnName = "total_amount",
                    Qualifier = qualifier,
                    Operator = "/",
                    ConstantValue = 2,
                    IsColumnOnLeft = isColumnOnLeft,
                    Location = "select_stmt / where_clause",
                    SourceOffset = 45
                });

            var findings = new ArithmeticOnColumnRule().Analyze(statement);

            var finding = Assert.Single(findings);

            Assert.Equal("ARITHMETIC_ON_COLUMN", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);

            Assert.Contains(expectedExpression, finding.Description);
            Assert.Contains("select_stmt / where_clause", finding.Description);

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

            var findings = new ArithmeticOnColumnRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_NullStatement_ThrowsArgumentNullException()
        {
            var rule = new ArithmeticOnColumnRule();

            Assert.Throws<ArgumentNullException>(() => rule.Analyze(null!));
        }
    }
}