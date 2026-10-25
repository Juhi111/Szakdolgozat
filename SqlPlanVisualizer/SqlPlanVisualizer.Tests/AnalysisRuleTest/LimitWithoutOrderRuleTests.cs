using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class LimitWithoutOrderRuleTests
    {
        [Fact]
        public void Analyze_MixedOccurrences_ReturnsOnlyLimitWithoutOrderFinding()
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

            statement.LimitOccurrences.Add(new LimitOccurrence
            {
                RowLimit = 10,
                HasOrderBy = true,
                Location = "select_stmt",
                SourceOffset = 90
            });

            var finding = Assert.Single(new LimitWithoutOrderRule().Analyze(statement));

            Assert.Equal("LIMIT_WITHOUT_ORDER", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
        }
    }
}