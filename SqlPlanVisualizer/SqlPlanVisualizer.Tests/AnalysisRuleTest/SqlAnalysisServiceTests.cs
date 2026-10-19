using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class SqlAnalysisServiceTests
    {
        [Fact]
        public void Analyze_MultipleStatements_CollectsAllFindings()
        {
            
            var firstStatement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            firstStatement.WildcardProjections.Add(
                new WildcardProjectionOccurrence
                {
                    Location = "select_stmt",
                    Qualifier = "o"
                });

            var secondStatement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            secondStatement.WildcardProjections.Add(
                new WildcardProjectionOccurrence
                {
                    Location = "select_stmt",
                    Qualifier = "a"
                });

            var service = new SqlAnalysisService(
                new ISqlAnalysisRule[]
                {
                    new SelectStarRule()
                });

            
            var findings = service.Analyze(
                new[] { firstStatement, secondStatement });

            
            Assert.Collection(
                findings,
                first =>
                {
                    Assert.Equal(1, first.StatementIndex);
                    Assert.Equal("SELECT_STAR", first.RuleId);
                },
                second =>
                {
                    Assert.Equal(2, second.StatementIndex);
                    Assert.Equal("SELECT_STAR", second.RuleId);
                });
        }
    }
}