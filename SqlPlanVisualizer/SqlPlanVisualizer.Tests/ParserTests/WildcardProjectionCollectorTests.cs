using Google.Protobuf;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class WildcardProjectionCollectorTests
    {
        [Fact]
        public void Collect_NestedSelects_ReturnsBothWildcards()
        {
            string sql ="SELECT o.* FROM orders o WHERE order_id IN (SELECT a.* FROM archived_orders a);";

            var parsed = new PostgreSqlQueryParser().Parse(sql);
            var statement = Assert.Single(parsed.Stmts);

            var occurrences = CollectWildcards(statement.Stmt, 1);

            Assert.Equal(2, occurrences.Count);
            Assert.Single(occurrences, occurrence => occurrence.Qualifier == "o");
            Assert.Single(occurrences, occurrence => occurrence.Qualifier == "a");
        }

        [Fact]
        public void Collect_QualifiedWildcard_ReturnsSourceOffset()
        {
            var parsed = new PostgreSqlQueryParser().Parse("SELECT o.* FROM orders o;");

            var statement = Assert.Single(parsed.Stmts);

            var occurrences = CollectWildcards(statement.Stmt, 1);
            var occurrence = Assert.Single(occurrences);

            Assert.Equal("o", occurrence.Qualifier);
            Assert.Equal(7, occurrence.SourceOffset);
        }

        [Fact]
        public void Collect_SecondStatement_ReturnsOffsetInFullSql()
        {
            string sql = "SELECT 1; SELECT o.* FROM orders o;";

            var parsed = new PostgreSqlQueryParser().Parse(sql);
            Assert.Equal(2, parsed.Stmts.Count);

            var occurrences = CollectWildcards(parsed.Stmts[1].Stmt, 2);
            var occurrence = Assert.Single(occurrences);

            Assert.Equal("o", occurrence.Qualifier);
            Assert.Equal(17, occurrence.SourceOffset);
        }

        private static List<WildcardProjectionOccurrence> CollectWildcards(IMessage root, int statementIndex)
        {
            var summary = new SqlStatementSummary
            {
                StatementIndex = statementIndex,
                StatementType = "SELECT"
            };

            var collector = new WildcardProjectionCollector();
            var walker = new PostgreSqlAstWalker();

            walker.Walk(root, statementIndex, entry =>
            {
                collector.Collect(entry, summary);
            });

            return summary.WildcardProjections;
        }
    }
}