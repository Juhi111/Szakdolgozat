using SqlPlanVisualizer.Core.Analysis.Rules;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class SelectStarRuleTests
    {
        [Fact]
        public void Analyze_WithWildcard_ReturnsFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 3,
                StatementType = "SELECT"
            };

            statement.WildcardProjections.Add(new WildcardProjectionOccurrence
            {
                Location = "select_stmt",
                Qualifier = "o",
                SourceOffset = 7
            });

            var finding = Assert.Single(new SelectStarRule().Analyze(statement));

            Assert.Equal("SELECT_STAR", finding.RuleId);
            Assert.Equal(3, finding.StatementIndex);
            Assert.Equal((int?)7, finding.SourceOffset);
            Assert.Contains("o.*", finding.Description);
        }
    }
}