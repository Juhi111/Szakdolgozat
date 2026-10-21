using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class DistinctRuleTests
    {
        [Fact]
        public void Analyze_PlainDistinct_ReturnsFinding() {
            var summary = new SqlStatementSummary();
            summary.StatementType = "SELECT";
            summary.StatementIndex = 2;
            summary.DistinctOccurrences.Add(new DistinctOccurrence(){Location = "select_stmt", IsDistinctOn = false });

            var findings = new DistinctRule().Analyze(summary);
            var finding = Assert.Single(findings);
            Assert.Equal(2, finding.StatementIndex);
            Assert.Equal("DISTINCT_USAGE", finding.RuleId);
        }

        [Fact]
        public void Analyze_DistinctOn_ReturnsNoFindings()
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            statement.DistinctOccurrences.Add(new DistinctOccurrence
            {
                Location = "select_stmt",
                IsDistinctOn = true
            });

            var findings = new DistinctRule().Analyze(statement);

            Assert.Empty(findings);
        }

        [Fact]
        public void Analyze_DistinctOnThenPlainDistinct_ReturnsPlainDistinctFinding() { 
            
            var summary = new SqlStatementSummary();
            summary.DistinctOccurrences.Add(new DistinctOccurrence() {IsDistinctOn = true, Location = "outer" } );
            summary.DistinctOccurrences.Add(new DistinctOccurrence() {IsDistinctOn = false, Location = "inner" });

            var findings = new DistinctRule().Analyze(summary);

            var finding = Assert.Single(findings);

            Assert.Equal("DISTINCT_USAGE", finding.RuleId);
            Assert.Contains("inner", finding.Description);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Analyze_PlainDistinct_MentionsJoinOnlyWhenPresent(bool hasJoin)
        {
            var statement = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            statement.DistinctOccurrences.Add(new DistinctOccurrence
            {
                Location = "select_stmt",
                IsDistinctOn = false,
                HasJoin = hasJoin
            });

            var findings = new DistinctRule().Analyze(statement);
            var finding = Assert.Single(findings);

            if (hasJoin)
                Assert.Contains("JOIN", finding.Recommendation);
            else
                Assert.DoesNotContain("JOIN", finding.Recommendation);
        }
    }
}
