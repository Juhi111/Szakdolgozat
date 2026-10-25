using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class NotInSubqueryRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.NotInSubqueryOccurrences.Add(new NotInSubqueryOccurrence
            {
                ColumnName = "customer_id",
                Qualifier = "c",
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new NotInSubqueryRule().Analyze(statement));

            Assert.Equal("NOT_IN_SUBQUERY", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("c.customer_id", finding.Description);
        }
    }
}