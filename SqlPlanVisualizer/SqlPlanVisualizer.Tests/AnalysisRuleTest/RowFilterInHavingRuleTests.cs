using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class RowFilterInHavingRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.RowFiltersInHaving.Add(new RowFilterInHavingOccurrence
            {
                ColumnName = "customer_id",
                Qualifier = "o",
                Operator = ">",
                ConstantValue = 100,
                IsColumnOnLeft = false,
                Location = "select_stmt / having_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new RowFilterInHavingRule().Analyze(statement));

            Assert.Equal("ROW_FILTER_IN_HAVING", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("100 > o.customer_id", finding.Description);
        }
    }
}