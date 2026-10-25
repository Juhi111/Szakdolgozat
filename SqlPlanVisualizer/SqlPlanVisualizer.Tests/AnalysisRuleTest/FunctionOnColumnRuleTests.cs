using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class FunctionOnColumnRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.FunctionOnColumnOccurrences.Add(new FunctionOnColumnOccurrence
            {
                FunctionName = "lower",
                ColumnName = "email",
                Qualifier = "c",
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new FunctionOnColumnRule().Analyze(statement));

            Assert.Equal("FUNCTION_ON_COLUMN", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("lower(c.email)", finding.Description);
        }
    }
}