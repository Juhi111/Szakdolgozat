using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class CastOnColumnRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.CastOnColumnOccurrences.Add(new CastOnColumnOccurrence
            {
                ColumnName = "customer_id",
                Qualifier = "c",
                TargetTypeName = "bigint",
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new CastOnColumnRule().Analyze(statement));

            Assert.Equal("CAST_ON_COLUMN", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("c.customer_id", finding.Description);
            Assert.Contains("bigint", finding.Description);
        }
    }
}