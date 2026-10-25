using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class OptionalParameterFilterRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 3,
                StatementType = "SELECT"
            };

            statement.OptionalParameterFilters.Add(new OptionalParameterFilterOccurrence
            {
                ParameterNumber = 2,
                ColumnName = "customer_id",
                Qualifier = "c",
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new OptionalParameterFilterRule().Analyze(statement));

            Assert.Equal("OPTIONAL_PARAMETER_FILTER", finding.RuleId);
            Assert.Equal(3, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("$2 IS NULL OR c.customer_id = $2", finding.Description);
        }
    }
}