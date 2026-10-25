using System.Collections.Generic;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class ConcatenationInPredicateRuleTests
    {
        [Fact]
        public void Analyze_WithOccurrence_ReturnsExpectedFinding()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 2,
                StatementType = "SELECT"
            };

            statement.ConcatenationOccurrences.Add(new ConcatenationOccurrence
            {
                ColumnReferences = new List<string> { "c.first_name", "c.last_name" },
                Location = "select_stmt / where_clause",
                SourceOffset = 45
            });

            var finding = Assert.Single(new ConcatenationInPredicateRule().Analyze(statement));

            Assert.Equal("CONCATENATION_IN_PREDICATE", finding.RuleId);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal((int?)45, finding.SourceOffset);
            Assert.Contains("c.first_name", finding.Description);
            Assert.Contains("c.last_name", finding.Description);
        }
    }
}