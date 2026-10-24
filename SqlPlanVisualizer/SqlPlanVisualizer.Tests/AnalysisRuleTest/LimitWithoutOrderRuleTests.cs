using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class LimitWithoutOrderRuleTests
    {
        [Fact]
        public void Analyze_WithoutOrderBy_ReturnsFindingWithSourceOffset()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.LimitOccurrences.Add(new LimitOccurrence
            {
                RowLimit = 20,
                HasOrderBy = false,
                Location = "select_stmt",
                SourceOffset = 45
            });

            var findings = new LimitWithoutOrderRule().Analyze(statement);
            var finding = Assert.Single(findings);

            Assert.Equal("LIMIT_WITHOUT_ORDER", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
        }

        [Fact]
        public void Analyze_WithOrderBy_ReturnsNoFindings()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            statement.LimitOccurrences.Add(new LimitOccurrence
            {
                RowLimit = 20,
                HasOrderBy = true,
                Location = "select_stmt"
            });

            var findings = new LimitWithoutOrderRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_WithoutLimit_ReturnsNoFindings()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            var findings = new LimitWithoutOrderRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_NullStatement_ThrowsArgumentNullException()
        {
            var rule = new LimitWithoutOrderRule();

            Assert.Throws<ArgumentNullException>(
                () => rule.Analyze(null!));
        }
    }
}