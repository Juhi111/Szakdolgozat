using System.Collections.Generic;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class PossibleCartesianProductRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.PossibleCartesianProducts.Add(new PossibleCartesianProductOccurrence
            {
                Relations = new List<string> { "customers AS c", "orders AS o" },
                Location = "select_stmt",
                SourceOffset = 45
            });

            var finding = Assert.Single(new PossibleCartesianProductRule().Analyze(statement));

            Assert.Equal("POSSIBLE_CARTESIAN_PRODUCT", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("customers AS c", finding.Description);
            Assert.Contains("orders AS o", finding.Description);
        }
    }
}