using SqlPlanVisualizer.Infrastructure.PostgreSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class WildcardProjectionCollectorTests
    {
        [Fact]
        public void Collect_NestedSelects_ReturnsBothWildcards()
        {
            string sql =
                "SELECT o.* FROM orders o " +
                "WHERE order_id IN (SELECT a.* FROM archived_orders a);";

            var parser = new PostgreSqlQueryParser();
            var parsed = parser.Parse(sql);
            var statement = Assert.Single(parsed.Stmts);

            var collector = new WildcardProjectionCollector();

            var occurrences = collector.Collect(statement.Stmt, 1);

            Assert.Equal(2, occurrences.Count);
            Assert.Single(occurrences, occurrence => occurrence.Qualifier == "o");
            Assert.Single(occurrences, occurrence => occurrence.Qualifier == "a");
        }

        [Fact]
        public void Collect_QualifiedWildcard_ReturnsSourceOffset()
        {
            var parsed = new PostgreSqlQueryParser().Parse("SELECT o.* FROM orders o;");

            var statement = Assert.Single(parsed.Stmts);
            var occurrences = new WildcardProjectionCollector().Collect(statement.Stmt, 1);

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

            var occurrences = new WildcardProjectionCollector().Collect(parsed.Stmts[1].Stmt, 2);

            var occurrence = Assert.Single(occurrences);

            Assert.Equal("o", occurrence.Qualifier);
            Assert.Equal(17, occurrence.SourceOffset);
        }
    }
}
