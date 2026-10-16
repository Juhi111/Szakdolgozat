using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class PostgreSqlQueryServiceTests
    {
        [Theory]
        [InlineData("SELECT * FROM orders;", true)]
        [InlineData("SELECT o.* FROM orders o;", true)]
        [InlineData("SELECT order_id FROM orders;", false)]
        [InlineData("SELECT COUNT(*) FROM orders;", false)]
        [InlineData("SELECT '*' FROM orders;", false)]
        [InlineData("SELECT * FROM orders UNION ALL SELECT order_id FROM archived_orders;", true)]
        [InlineData("SELECT order_id FROM orders UNION ALL SELECT * FROM archived_orders;", true)]
        [InlineData("SELECT order_id FROM orders UNION ALL SELECT order_id FROM archived_orders;", false)]
        [InlineData("SELECT order_id FROM orders UNION ALL SELECT order_id FROM archived_orders UNION ALL SELECT * FROM old_orders;", true)]
        [InlineData("SELECT s.order_id FROM (SELECT * FROM orders) AS s;", true)]
        [InlineData("SELECT s.order_id FROM (SELECT order_id FROM orders) AS s;", false)]
        [InlineData("SELECT s.total FROM (SELECT COUNT(*) AS total FROM orders) AS s;", false)]
        [InlineData("SELECT s.order_id FROM (SELECT t.order_id FROM (SELECT * FROM orders) AS t) AS s;", true)]
        [InlineData("SELECT o.order_id FROM orders o, (SELECT * FROM customers) AS c;", true)]
        [InlineData("SELECT o.order_id FROM orders o JOIN (SELECT * FROM customers) c ON c.customer_id = o.customer_id;", true)]
        [InlineData("SELECT o.order_id FROM (SELECT * FROM orders) o JOIN customers c ON c.customer_id = o.customer_id;", true)]
        [InlineData("SELECT o.order_id FROM orders o JOIN (SELECT customer_id FROM customers) c ON c.customer_id = o.customer_id;", false)]
        [InlineData("WITH s AS (SELECT * FROM orders) SELECT order_id FROM s;", true)]
        [InlineData("WITH s AS (SELECT order_id FROM orders) SELECT order_id FROM s;", false)]
        [InlineData("WITH a AS (SELECT order_id FROM orders), b AS (SELECT * FROM a) SELECT order_id FROM b;",true)]
        public void ParseSummary_SelectProjection_ReturnsExpectedWildcardFlag(string sql, bool expectedWildcardFlag) {          

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);
            Assert.True(summary.IsSuccess);
            var statement = Assert.Single(summary.Statements);
            Assert.Equal("SELECT", statement.StatementType);
            Assert.Equal(expectedWildcardFlag, statement.HasWildcardProjection);
        }

        [Fact]
        public void ParseSummary_CteWildcard_ReturnsLocationAndQualifier()
        {
            string sql = "WITH rendelesek AS (SELECT o.* FROM orders o) SELECT order_id FROM rendelesek;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("Fő lekérdezés / CTE[rendelesek]", wildcard.Location);
            Assert.Equal("o", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_UnionWildcards_ReturnsBothOccurrences() { 
            string sql = "SELECT o.* FROM orders o UNION ALL SELECT a.* FROM archived_orders a;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            Assert.Equal(2, statement.WildcardProjections.Count);

            var firstWildcard = statement.WildcardProjections[0];
            Assert.Equal("Fő lekérdezés / bal ág", firstWildcard.Location);
            Assert.Equal("o", firstWildcard.Qualifier);

            var secondWildcard = statement.WildcardProjections[1];
            Assert.Equal("Fő lekérdezés / jobb ág", secondWildcard.Location);
            Assert.Equal("a", secondWildcard.Qualifier);
        }
    }
}
