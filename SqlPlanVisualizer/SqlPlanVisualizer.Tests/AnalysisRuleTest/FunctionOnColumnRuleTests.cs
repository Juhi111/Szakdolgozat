using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using System;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class FunctionOnColumnRuleTests
    {
        [Theory]
        [InlineData("lower", null, "lower(email)")]
        [InlineData("upper", "c", "upper(c.email)")]
        [InlineData("lower", "public.customers", "lower(public.customers.email)")]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding(string functionName, string? qualifier, string expectedExpression)
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.FunctionOnColumnOccurrences.Add(
                new FunctionOnColumnOccurrence
                {
                    FunctionName = functionName,
                    ColumnName = "email",
                    Qualifier = qualifier,
                    Location = "select_stmt / where_clause",
                    SourceOffset = 45
                });

            var findings = new FunctionOnColumnRule().Analyze(statement);

            var finding = Assert.Single(findings);

            Assert.Equal("FUNCTION_ON_COLUMN", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);

            Assert.Contains(expectedExpression, finding.Description);
            Assert.Contains("select_stmt / where_clause", finding.Description);

            Assert.False(string.IsNullOrWhiteSpace(finding.Title));
            Assert.False(string.IsNullOrWhiteSpace(finding.Recommendation));
        }

        [Fact]
        public void Analyze_MultipleOccurrences_ReturnsFindingForEach()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 3,
                StatementType = "SELECT"
            };

            statement.FunctionOnColumnOccurrences.Add(
                new FunctionOnColumnOccurrence
                {
                    FunctionName = "lower",
                    ColumnName = "email",
                    SourceOffset = 40
                });

            statement.FunctionOnColumnOccurrences.Add(
                new FunctionOnColumnOccurrence
                {
                    FunctionName = "upper",
                    ColumnName = "name",
                    Qualifier = "c",
                    SourceOffset = 80
                });

            var findings = new FunctionOnColumnRule().Analyze(statement);

            Assert.Collection(findings,
                first =>
                {
                    Assert.Equal("FUNCTION_ON_COLUMN", first.RuleId);
                    Assert.Equal(3, first.StatementIndex);
                    Assert.Equal((int?)40, first.SourceOffset);
                    Assert.Contains("lower(email)", first.Description);
                },
                second =>
                {
                    Assert.Equal("FUNCTION_ON_COLUMN", second.RuleId);
                    Assert.Equal(3, second.StatementIndex);
                    Assert.Equal((int?)80, second.SourceOffset);
                    Assert.Contains("upper(c.name)", second.Description);
                }
            );
        }

        [Fact]
        public void Analyze_MissingSourceOffset_PreservesNull()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            statement.FunctionOnColumnOccurrences.Add(
                new FunctionOnColumnOccurrence
                {
                    FunctionName = "lower",
                    ColumnName = "email",
                    SourceOffset = null
                });

            var findings = new FunctionOnColumnRule().Analyze(statement);

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

            var findings = new FunctionOnColumnRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_NullStatement_ThrowsArgumentNullException()
        {
            var rule = new FunctionOnColumnRule();

            Assert.Throws<ArgumentNullException>(() => rule.Analyze(null!));
        }
    }
}