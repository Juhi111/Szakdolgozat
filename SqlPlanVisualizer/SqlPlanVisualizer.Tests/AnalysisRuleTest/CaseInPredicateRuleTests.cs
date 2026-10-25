using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class CaseInPredicateRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 3,
                StatementType = "SELECT"
            };

            statement.CaseInPredicateOccurrences.Add(new CaseInPredicateOccurrence
            {
                SelectorParameterNumber = 1,
                ComparisonParameterNumber = 2,
                ThenColumn = "c.email",
                ElseColumn = "c.phone",
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new CaseInPredicateRule().Analyze(statement));

            Assert.Equal("CASE_IN_PREDICATE", finding.RuleId);
            Assert.Equal(3, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("c.email", finding.Description);
            Assert.Contains("c.phone", finding.Description);
            Assert.Contains("$1", finding.Description);
            Assert.Contains("$2", finding.Description);
        }
    }
}