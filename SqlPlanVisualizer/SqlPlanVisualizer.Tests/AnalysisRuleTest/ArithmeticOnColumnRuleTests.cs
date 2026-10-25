using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class ArithmeticOnColumnRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.ArithmeticOnColumnOccurrences.Add(new ArithmeticOnColumnOccurrence
            {
                ColumnName = "total_amount",
                Qualifier = "o",
                Operator = "/",
                ConstantValue = 2,
                IsColumnOnLeft = true,
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new ArithmeticOnColumnRule().Analyze(statement));

            Assert.Equal("ARITHMETIC_ON_COLUMN", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("o.total_amount / 2", finding.Description);
        }
    }
}