using System;
using System.Collections.Generic;
using System.Linq;
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
        [InlineData("WITH a AS (SELECT order_id FROM orders), b AS (SELECT * FROM a) SELECT order_id FROM b;", true)]
        public void ParseSummary_SelectProjection_ReturnsExpectedWildcardFlag(string sql, bool expectedWildcardFlag)
        {

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);
            Assert.True(summary.IsSuccess, summary.ErrorMessage);
            var statement = Assert.Single(summary.Statements);
            Assert.Equal("SELECT", statement.StatementType);
            Assert.Equal(expectedWildcardFlag, statement.HasWildcardProjection);
        }

        [Fact]
        public void ParseSummary_CteWildcard_ReturnsQualifier()
        {
            string sql = "WITH rendelesek AS (SELECT o.* FROM orders o) SELECT order_id FROM rendelesek;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("o", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_UnionWildcards_ReturnsBothOccurrences()
        {
            string sql = "SELECT o.* FROM orders o UNION ALL SELECT a.* FROM archived_orders a;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            Assert.Equal(2, statement.WildcardProjections.Count);

            var firstWildcard = statement.WildcardProjections[0];
            Assert.Equal("o", firstWildcard.Qualifier);

            var secondWildcard = statement.WildcardProjections[1];
            Assert.Equal("a", secondWildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_ScalarSubqueryWildcard_ReturnsOccurrence()
        {

            string sql = "SELECT (SELECT o.* FROM orders o LIMIT 1)";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("o", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_WhereInSubqueryWildcard_ReturnsOccurrence()
        {
            string sql = "SELECT order_id FROM orders WHERE order_id IN (SELECT *FROM archived_orders\r\n);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Null(wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_AndSubqueryWildcard_ReturnsOccurrence()
        {
            string sql = "SELECT order_id FROM orders WHERE customer_id = 10 AND order_id IN (SELECT a.* FROM archived_orders a);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_OrSubqueries_ReturnsBothOccurrences()
        {
            string sql = "SELECT order_id FROM orders WHERE order_id IN (SELECT a.* FROM archived_orders a) OR order_id IN (SELECT p.* FROM pending_orders p);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Equal(2, statement.WildcardProjections.Count);

            var firstWildcard = statement.WildcardProjections[0];

            var secondWildcard = statement.WildcardProjections[1];

            Assert.Equal("a", firstWildcard.Qualifier);
            Assert.Equal("p", secondWildcard.Qualifier);
        }

        [Theory]
        [InlineData("SELECT order_id FROM orders WHERE order_id = (SELECT a.* FROM archived_orders a LIMIT 1);")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_orders a LIMIT 1) = order_id;")]
        public void ParseSummary_ComparisonSubquery_ReturnsOccurrence(string sql)
        {

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_FunctionArgumentSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT ABS((SELECT a.* FROM archived_orders a LIMIT 1));";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_FunctionArguments_ReturnsBothOccurrences()
        {
            string sql = "SELECT power((SELECT a.* FROM archived_orders a LIMIT 1), (SELECT p.* FROM pending_orders p LIMIT 1));";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);
            var statement = Assert.Single(summary.Statements);

            Assert.Equal(2, statement.WildcardProjections.Count);

            var firstWildcard = statement.WildcardProjections[0];
            var secondWildcard = statement.WildcardProjections[1];
            Assert.Equal("a", firstWildcard.Qualifier);
            Assert.Equal("p", secondWildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_JoinOnSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT o.order_id FROM orders o JOIN customers c ON c.customer_id = (SELECT a.* FROM archived_customers a LIMIT 1);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_HavingSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT customer_id, COUNT(*) FROM orders GROUP BY customer_id HAVING COUNT(*) > (SELECT a.* FROM archived_counts a LIMIT 1);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_OrderBySubquery_ReturnsOccurrence()
        {
            string sql = "SELECT order_id FROM orders ORDER BY (SELECT a.* FROM archived_orders a LIMIT 1);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_SecondOrderByItemSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT order_id FROM orders ORDER BY order_id, (SELECT a.* FROM archived_orders a LIMIT 1);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_TypeCastSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT (SELECT a.* FROM archived_orders a LIMIT 1)::text;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_CaseThenSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT CASE WHEN order_id > 0 THEN (SELECT a.* FROM archived_orders a LIMIT 1) ELSE NULL END FROM orders;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Theory]
        [InlineData("SELECT CASE WHEN (SELECT a.* FROM archived_orders a LIMIT 1) > 0 THEN 1 ELSE 0 END;")]
        [InlineData("SELECT CASE WHEN false THEN 0 ELSE (SELECT a.* FROM archived_orders a LIMIT 1) END;")]
        [InlineData("SELECT CASE (SELECT a.* FROM archived_orders a LIMIT 1) WHEN 1 THEN 'egy' ELSE 'mas' END;")]
        public void ParseSummary_CaseSubquery_ReturnsOccurrence(string sql)
        {

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_CoalesceSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT COALESCE((SELECT a.* FROM archived_orders a LIMIT 1),0);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Theory]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_orders a LIMIT 1) IS NULL;")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_orders a LIMIT 1) IS NOT NULL;")]
        public void ParseSummary_NullTestSubquery_ReturnsOccurrence(string sql)
        {

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("a", wildcard.Qualifier);
        }

        [Theory]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_flags a LIMIT 1) IS TRUE;")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_flags a LIMIT 1) IS NOT TRUE;")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_flags a LIMIT 1) IS FALSE;")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_flags a LIMIT 1) IS NOT FALSE;")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_flags a LIMIT 1) IS UNKNOWN;")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_flags a LIMIT 1) IS NOT UNKNOWN;")]
        public void ParseSummary_BooleanTestSubquery_ReturnsOccurrence(string sql)
        {
            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_GroupBySubquery_ReturnsOccurrence()
        {
            string sql = "SELECT COUNT(*) FROM orders GROUP BY (SELECT a.* FROM archived_groups a LIMIT 1);";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_RollupSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT COUNT(*) FROM orders GROUP BY ROLLUP( (SELECT a.* FROM archived_groups a LIMIT 1));";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_NestedGroupingSets_ReturnsOccurrence()
        {
            string sql = "SELECT COUNT(*) FROM orders GROUP BY GROUPING SETS (ROLLUP((SELECT a.* FROM archived_groups a LIMIT 1)), ());";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_DistinctOnSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT DISTINCT ON ((SELECT a.* FROM archived_groups a LIMIT 1)) order_id FROM orders;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_PlainDistinctWithoutWildcard_ReturnsNoOccurrences()
        {
            string sql = "SELECT DISTINCT order_id FROM orders;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            Assert.Empty(statement.WildcardProjections);
        }

        [Theory]
        [InlineData("SELECT order_id FROM orders LIMIT (SELECT a.* FROM query_limits a LIMIT 1);")]
        [InlineData("SELECT order_id FROM orders OFFSET (SELECT a.* FROM query_offsets a LIMIT 1);")]
        public void ParseSummary_LimitOffsetSubquery_ReturnsOccurrence(string sql)
        {
            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var wildcard = Assert.Single(statement.WildcardProjections);
            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_AggregateFilterSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT COUNT(*) FILTER (WHERE order_id > (SELECT a.* FROM archived_limits a LIMIT 1)) FROM orders;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_AggregateOrderBySubquery_ReturnsOccurrence()
        {
            string sql = "SELECT array_agg(order_id ORDER BY (SELECT a.* FROM archived_priorities a LIMIT 1))FROM orders;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_WindowPartitionBySubquery_ReturnsOccurrence()
        {
            string sql = "SELECT row_number() OVER (PARTITION BY (SELECT a.* FROM archived_groups a LIMIT 1) ) FROM orders;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_WindowOrderBySubquery_ReturnsOccurrence()
        {
            string sql = "SELECT row_number() OVER (ORDER BY (SELECT a.* FROM archived_priorities a LIMIT 1)) FROM orders;";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }

        [Fact]
        public void ParseSummary_NamedWindowSubquery_ReturnsOccurrence()
        {
            string sql = "SELECT row_number() OVER w FROM orders WINDOW w AS (PARTITION BY (SELECT a.* FROM archived_groups a LIMIT 1));";

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            var wildcard = Assert.Single(statement.WildcardProjections);

            Assert.Equal("a", wildcard.Qualifier);
        }
    }      
}
