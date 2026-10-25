using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class YearExtractionInPredicateRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.YearExtractionOccurrences.Add(new YearExtractionOccurrence
            {
                FunctionName = "extract",
                ColumnName = "created_at",
                Qualifier = null,
                ComparisonOperator = "=",
                IsExtractionOnLeft = true,
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new YearExtractionInPredicateRule().Analyze(statement));

            Assert.Equal("YEAR_EXTRACTION_IN_PREDICATE", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("EXTRACT(YEAR FROM created_at)", finding.Description);
        }
    }
}