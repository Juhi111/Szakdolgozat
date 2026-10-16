using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Analysis;
using Xunit;

namespace SqlPlanVisualizer.Tests.AnalysisRuleTest
{
    public class SelectStarRuleTests
    {
        [Fact]
        public void Analyze_WithWildcard_ReturnsFinding()
        {
            // Bemenet: egy utasítás, benne egy o.* előfordulás.
            var sum = new SqlStatementSummary
            {
                StatementIndex = 3,
                StatementType = "SELECT"
            };

            sum.WildcardProjections.Add(new WildcardProjectionOccurrence
            {
                Location = "Fő lekérdezés",
                Qualifier = "o"
            });

            var results = new SelectStarRule().Analyze(sum);

            // Ellenőrzi, hogy pontosan egy elem van, és visszaadja azt.
            var finding = Assert.Single(results);

            Assert.Equal(3, finding.StatementIndex);
            Assert.Equal("SELECT_STAR", finding.RuleId);
            Assert.Contains("Fő lekérdezés", finding.Description);
            Assert.Contains("o.*", finding.Description);
        }

        [Fact]
        public void Analyze_WithoutWildcard_ReturnsEmptyList()
        {
            var sum = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            var results = new SelectStarRule().Analyze(sum);

            Assert.Empty(results);
        }

        [Fact]
        public void Analyze_WithMultipleWildcards_ReturnsFindingForEachOccurrence()
        {
            var sum = new SqlStatementSummary
            {
                StatementIndex = 1,
                StatementType = "SELECT"
            };

            sum.WildcardProjections.Add(new WildcardProjectionOccurrence
            {
                Location = "Fő lekérdezés",
                Qualifier = "o"
            });

            sum.WildcardProjections.Add(new WildcardProjectionOccurrence
            {
                Location = "Fő lekérdezés / CTE[rendelesek]",
                Qualifier = null
            });

            var results = new SelectStarRule().Analyze(sum);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                "Hely: Fő lekérdezés. Csillagos oszlopkiválasztás: o.*.",
                results[0].Description);

            Assert.Equal(
                "Hely: Fő lekérdezés / CTE[rendelesek]. Csillagos oszlopkiválasztás: *.",
                results[1].Description);
        }

        [Fact]
        public void Analyze_NullStatement_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SelectStarRule().Analyze(null!));
        }
    }
}