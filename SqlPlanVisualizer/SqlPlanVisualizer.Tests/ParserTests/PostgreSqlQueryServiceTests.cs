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

        [Theory]
        [InlineData("SELECT customer_id FROM orders;", 0, false)]
        [InlineData("SELECT DISTINCT customer_id FROM orders;", 1, false)]
        [InlineData("SELECT DISTINCT ON (customer_id) customer_id, order_id FROM orders;", 1, true)]
        public void ParseSummary_Distinct_ReturnsExpectedOccurrences(string sql, int expectedCount, bool expectedIsDistinctOn)
        {
            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Equal(expectedCount, statement.DistinctOccurrences.Count);

            if (expectedCount > 0)
            {
                var occurrence = Assert.Single(statement.DistinctOccurrences);
                Assert.Equal(expectedIsDistinctOn, occurrence.IsDistinctOn);
            }
        }

        [Fact]
        public void ParseSummary_SubqueryDistinct_ReturnsOccurrence() {
            string sql = "SELECT customer_id FROM (SELECT DISTINCT customer_id FROM orders) AS unique_customers;";

            var service = new PostgreSqlQueryService();

            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);
            
            var statement = Assert.Single(summary.Statements);

            var occurrence = Assert.Single(statement.DistinctOccurrences);

            Assert.False(occurrence.IsDistinctOn);
        }

        [Theory]
        [InlineData("SELECT DISTINCT customer_id FROM customers;", false)]
        [InlineData("SELECT DISTINCT c.customer_id FROM customers c JOIN orders o ON o.customer_id = c.customer_id;", true)]
        [InlineData("SELECT DISTINCT q.customer_id FROM (SELECT c.customer_id FROM customers c JOIN orders o ON o.customer_id = c.customer_id) q;", false)]
        public void ParseSummary_Distinct_ReturnsExpectedHasJoin(string sql, bool expectedHasJoin){

            var service = new PostgreSqlQueryService();
            var summary = service.ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.DistinctOccurrences);

            Assert.Equal(expectedHasJoin, occurrence.HasJoin);
        }

        [Theory]
        [InlineData("SELECT id FROM orders WHERE cancelled_at = NULL;", 1)]
        [InlineData("SELECT id FROM orders WHERE NULL = cancelled_at;", 1)]
        [InlineData("SELECT id FROM orders WHERE cancelled_at <> NULL;", 1)]
        [InlineData("SELECT id FROM orders WHERE cancelled_at != NULL;", 1)]
        [InlineData("SELECT id FROM orders WHERE cancelled_at IS NULL;", 0)]
        [InlineData("SELECT id FROM orders WHERE cancelled_at IS NOT NULL;", 0)]
        [InlineData("SELECT id FROM orders WHERE cancelled_at IS DISTINCT FROM NULL;", 0)]
        [InlineData("SELECT id FROM orders WHERE cancelled_at IS NOT DISTINCT FROM NULL;", 0)]
        [InlineData("SELECT id FROM orders WHERE status = 'NULL';", 0)]
        [InlineData("SELECT id FROM orders WHERE customer_id = 10;", 0)]
        public void ParseSummary_NullComparison_ReturnsExpectedCount(string sql, int expectedCount)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Equal(expectedCount, statement.NullComparisons.Count);
        }

        [Theory]
        [InlineData("SELECT order_id FROM orders LIMIT 20;", false)]
        [InlineData("SELECT order_id FROM orders ORDER BY order_id LIMIT 20;", true)]
        public void ParseSummary_PositiveLimit_ReturnsExpectedOccurrence(string sql, bool expectedHasOrderBy)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.LimitOccurrences);

            Assert.Equal(20L, occurrence.RowLimit);
            Assert.Equal(expectedHasOrderBy, occurrence.HasOrderBy);
            Assert.Equal("select_stmt", occurrence.Location);

            int expectedOffset = sql.IndexOf("20", StringComparison.Ordinal);
            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Theory]
        [InlineData("SELECT order_id FROM orders;")]
        [InlineData("SELECT order_id FROM orders LIMIT 0;")]
        [InlineData("SELECT order_id FROM orders LIMIT ALL;")]
        [InlineData("SELECT order_id FROM orders LIMIT NULL;")]
        [InlineData("SELECT order_id FROM orders LIMIT $1;")]
        [InlineData("SELECT order_id FROM orders LIMIT (10 + 10);")]
        [InlineData("SELECT q.order_id FROM (SELECT order_id FROM orders LIMIT 20) q;")]
        public void ParseSummary_UnsupportedOrAbsentLimit_ReturnsNoOccurrences(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.LimitOccurrences);
        }

        [Theory]
        [InlineData("SELECT email FROM customers WHERE email LIKE 'adam';", "adam", false, false, false)]
        [InlineData("SELECT email FROM customers WHERE email LIKE '%adam%';", "%adam%", false, true, true)]
        [InlineData(   "SELECT email FROM customers WHERE email ILIKE '%adam%';","%adam%", true, true, true)]
        [InlineData("SELECT email FROM customers WHERE email LIKE 'adam%';", "adam%", false, true, false)]
        [InlineData("SELECT email FROM customers WHERE email LIKE '_adam';", "_adam", false, true, true)]
        [InlineData("SELECT email FROM customers WHERE email LIKE 'ad_m';","ad_m", false, true, false)]
        [InlineData("SELECT email FROM customers WHERE email LIKE '!%adam' ESCAPE '!';", "!%adam", false, false, false)]
        [InlineData("SELECT email FROM customers WHERE email LIKE 'adam!_%' ESCAPE '!';", "adam!_%", false, true, false)]
        [InlineData("SELECT email FROM customers WHERE email LIKE '!!%' ESCAPE '!';", "!!%", false, true, false)]
        [InlineData("SELECT email FROM customers WHERE email LIKE '%adam' ESCAPE '';", "%adam", false, true, true)]
        [InlineData("SELECT email FROM customers WHERE email LIKE '';", "", false, false, false)]
        public void ParseSummary_LikePattern_ReturnsExpectedOccurrence(string sql, string expectedPattern, bool expectedCaseInsensitive, bool expectedHasWildcard, bool expectedHasLeadingWildcard)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.LikeOccurrences);

            Assert.Equal(expectedPattern, occurrence.Pattern);
            Assert.Equal(expectedCaseInsensitive, occurrence.IsCaseInsensitive);
            Assert.Equal(expectedHasWildcard, occurrence.HasWildcard);
            Assert.Equal(expectedHasLeadingWildcard, occurrence.HasLeadingWildcard);

            Assert.False(string.IsNullOrWhiteSpace(occurrence.Location));
            
            string keyword = expectedCaseInsensitive ? "ILIKE" : "LIKE";
            int expectedOffset = sql.IndexOf(keyword, StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Theory]
        [InlineData("SELECT email FROM customers WHERE email LIKE E'\\\\%adam';", false)]
        [InlineData("SELECT email FROM customers WHERE email LIKE E'\\\\%adam' ESCAPE '';", true)]
        public void ParseSummary_BackslashEscape_ReturnsExpectedWildcard(string sql, bool expectedHasWildcard)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.LikeOccurrences);

            Assert.Equal("\\%adam", occurrence.Pattern);
            Assert.Equal(expectedHasWildcard, occurrence.HasWildcard);

            
            Assert.False(occurrence.HasLeadingWildcard);
        }

        [Theory]
        [InlineData("SELECT email FROM customers WHERE email LIKE '%adam%';")]
        [InlineData("SELECT a.email FROM customers a JOIN customers b ON a.email LIKE '%adam%';")]
        [InlineData("SELECT email FROM customers WHERE email IS NOT NULL AND (email LIKE '%adam%');")]
        public void ParseSummary_LikeInSupportedClause_ReturnsOccurrence(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.LikeOccurrences);

            Assert.Equal("%adam%", occurrence.Pattern);
            Assert.True(occurrence.HasLeadingWildcard);
        }

        [Theory]
        [InlineData( "SELECT email FROM customers;")]
        [InlineData("SELECT email FROM customers WHERE email NOT LIKE '%adam%';")]
        [InlineData("SELECT email FROM customers WHERE email NOT ILIKE '%adam%';")]
        [InlineData("SELECT email FROM customers WHERE email LIKE $1;")]
        [InlineData("SELECT email FROM customers WHERE email LIKE NULL;")]
        [InlineData("SELECT email FROM customers WHERE email LIKE ('ad' || 'am');")]
        [InlineData("SELECT email FROM customers WHERE email LIKE 'adam' ESCAPE $1;")]
        [InlineData("SELECT email LIKE '%adam%' FROM customers;")]
        [InlineData("SELECT email FROM customers ORDER BY email LIKE '%adam%';")]
        [InlineData("SELECT email FROM customers GROUP BY email HAVING email LIKE '%adam%';")]
        public void ParseSummary_UnsupportedLike_ReturnsNoOccurrences(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.LikeOccurrences);
        }

        [Fact]
        public void ParseSummary_LikeInInnerSelectList_ReturnsNoOccurrences()
        {
            string sql ="SELECT customer_id FROM customers WHERE EXISTS (SELECT email LIKE '%adam%' FROM orders);";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.LikeOccurrences);
        }

        [Fact]
        public void ParseSummary_LikeInInnerWhere_ReturnsOccurrence()
        {
            string sql ="SELECT (SELECT email FROM customers WHERE email LIKE '%adam%' LIMIT 1);";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.LikeOccurrences);

            Assert.Equal("%adam%", occurrence.Pattern);
            Assert.True(occurrence.HasLeadingWildcard);

            int expectedOffset = sql.IndexOf("LIKE", StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_MultipleLikes_CollectsEveryOccurrence()
        {
            string sql ="SELECT email FROM customers WHERE email LIKE 'adam' OR email ILIKE '%eva%';";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Equal(2, statement.LikeOccurrences.Count);

            
            var exact = Assert.Single(statement.LikeOccurrences.Where(x => x.Pattern == "adam"));

            Assert.False(exact.IsCaseInsensitive);
            Assert.False(exact.HasWildcard);
            Assert.False(exact.HasLeadingWildcard);

            var leading = Assert.Single(statement.LikeOccurrences.Where(x => x.Pattern == "%eva%"));

            Assert.True(leading.IsCaseInsensitive);
            Assert.True(leading.HasWildcard);
            Assert.True(leading.HasLeadingWildcard);
        }

        [Fact]
        public void ParseSummary_MultipleStatements_KeepsLikesInOwnStatements()
        {
            string sql ="SELECT email FROM customers WHERE email LIKE 'adam'; SELECT email FROM customers WHERE email ILIKE '%eva%';";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            Assert.Collection(summary.Statements, first =>
                {
                    Assert.Equal(1, first.StatementIndex);
                    var occurrence = Assert.Single(first.LikeOccurrences);
                    Assert.Equal("adam", occurrence.Pattern);
                    Assert.False(occurrence.IsCaseInsensitive);
                    Assert.False(occurrence.HasWildcard);
                },
                second =>
                {
                    Assert.Equal(2, second.StatementIndex);

                    var occurrence = Assert.Single(second.LikeOccurrences);

                    Assert.Equal("%eva%", occurrence.Pattern);
                    Assert.True(occurrence.IsCaseInsensitive);
                    Assert.True(occurrence.HasLeadingWildcard);

                    int expectedOffset = sql.IndexOf("ILIKE", StringComparison.Ordinal);

                    Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
                }
            );
        }
        
        [Theory]
        [InlineData("SELECT email FROM customers WHERE email LIKE 'adam' ESCAPE '!!';")]
        [InlineData("SELECT email FROM customers WHERE email LIKE 'adam!' ESCAPE '!';")]
        public void ParseSummary_InvalidLikeEscape_ReturnsNoOccurrences(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.LikeOccurrences);
        }

        [Theory]
        [InlineData("SELECT email FROM customers WHERE lower(email) = 'adam';", "lower", "email", null, "lower")]
        [InlineData("SELECT email FROM customers WHERE upper(email) = 'ADAM';", "upper", "email", null, "upper")]
        [InlineData("SELECT c.email FROM customers c WHERE lower(c.email) = 'adam';", "lower", "email", "c", "lower")]
        [InlineData("SELECT email FROM public.customers WHERE lower(public.customers.email) = 'adam';", "lower", "email", "public.customers", "lower")]
        [InlineData("SELECT email FROM customers  WHERE pg_catalog.lower(email) = 'adam';", "lower", "email", null, "pg_catalog.lower")]
        [InlineData("SELECT email FROM customers WHERE LOWER(email) = 'adam';", "lower", "email", null, "LOWER")]
        public void ParseSummary_FunctionOnColumn_ReturnsExpectedOccurrence(string sql, string expectedFunctionName, string expectedColumnName, string? expectedQualifier, string sourceFunctionName)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.FunctionOnColumnOccurrences);

            Assert.Equal(expectedFunctionName, occurrence.FunctionName);
            Assert.Equal(expectedColumnName, occurrence.ColumnName);
            Assert.Equal(expectedQualifier, occurrence.Qualifier);

            Assert.False(string.IsNullOrWhiteSpace(occurrence.Location));

            int expectedOffset = sql.IndexOf(sourceFunctionName, StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_FunctionInJoinOn_ReturnsOccurrence()
        {
            string sql ="SELECT a.email FROM customers a JOIN customers b ON lower(a.email) = b.email;";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.FunctionOnColumnOccurrences);

            Assert.Equal("lower", occurrence.FunctionName);
            Assert.Equal("email", occurrence.ColumnName);
            Assert.Equal("a", occurrence.Qualifier);

            int expectedOffset = sql.IndexOf("lower", StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Theory]
        [InlineData("SELECT email FROM customers;")]
        [InlineData("SELECT lower(email) FROM customers;")]
        [InlineData("SELECT email FROM customers ORDER BY lower(email);")]
        [InlineData("SELECT lower(email) FROM customers GROUP BY lower(email);")]
        [InlineData("SELECT email FROM customers GROUP BY email HAVING lower(email) = 'adam';")]
        [InlineData("SELECT email FROM customers WHERE lower('ADAM') = email;")]
        [InlineData("SELECT email FROM customers WHERE lower($1) = email;")]
        [InlineData("SELECT email FROM customers WHERE lower(NULL) = email;")]
        [InlineData("SELECT email FROM customers WHERE length(email) > 5;")]
        [InlineData("SELECT email FROM customers WHERE custom.lower(email) = 'adam';")]
        [InlineData("SELECT email FROM customers WHERE lower(email || '.hu') = 'adam.hu';")]
        [InlineData("SELECT email FROM customers WHERE lower(trim(email)) = 'adam';")]
        public void ParseSummary_UnsupportedFunctionOnColumn_ReturnsNoOccurrences(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.FunctionOnColumnOccurrences);
        }

        [Fact]
        public void ParseSummary_SupportedInnerFunction_ReturnsOccurrence()
        {
            string sql = "SELECT email FROM customers WHERE trim(lower(email)) = 'adam';";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.FunctionOnColumnOccurrences);

            Assert.Equal("lower", occurrence.FunctionName);
            Assert.Equal("email", occurrence.ColumnName);

            int expectedOffset = sql.IndexOf("lower", StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_FunctionInInnerSelectList_ReturnsNoOccurrences()
        {
            string sql ="SELECT customer_id FROM customers WHERE EXISTS (SELECT lower(reference) FROM orders);";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.FunctionOnColumnOccurrences);
        }

        [Fact]
        public void ParseSummary_FunctionInInnerWhere_ReturnsOccurrence()
        {
            string sql = "SELECT EXISTS (SELECT 1 FROM customers c WHERE lower(c.email) = 'adam');";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.FunctionOnColumnOccurrences);

            Assert.Equal("lower", occurrence.FunctionName);
            Assert.Equal("email", occurrence.ColumnName);
            Assert.Equal("c", occurrence.Qualifier);

            int expectedOffset = sql.IndexOf("lower", StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_MultipleFunctions_CollectsEveryOccurrence()
        {
            string sql = "SELECT email FROM customers WHERE lower(email) = 'adam' AND upper(name) = 'ADAM';";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Equal(2, statement.FunctionOnColumnOccurrences.Count);

            var lower = Assert.Single(statement.FunctionOnColumnOccurrences.Where(x => x.FunctionName == "lower"));

            Assert.Equal("email", lower.ColumnName);

            var upper = Assert.Single(statement.FunctionOnColumnOccurrences.Where(x => x.FunctionName == "upper"));

            Assert.Equal("name", upper.ColumnName);
        }

        [Fact]
        public void ParseSummary_MultipleStatements_KeepsFunctionsInOwnStatements()
        {
            string sql = "SELECT email FROM customers WHERE lower(email) = 'adam'; SELECT name FROM customers WHERE upper(name) = 'EVA';";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            Assert.Collection(summary.Statements,
                first =>
                {
                    Assert.Equal(1, first.StatementIndex);

                    var occurrence =
                        Assert.Single(first.FunctionOnColumnOccurrences);

                    Assert.Equal("lower", occurrence.FunctionName);
                    Assert.Equal("email", occurrence.ColumnName);
                },
                second =>
                {
                    Assert.Equal(2, second.StatementIndex);

                    var occurrence =
                        Assert.Single(second.FunctionOnColumnOccurrences);

                    Assert.Equal("upper", occurrence.FunctionName);
                    Assert.Equal("name", occurrence.ColumnName);

                    int expectedOffset =
                        sql.IndexOf("upper", StringComparison.Ordinal);

                    Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
                }
            );
        }

        [Theory]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id::text = '123';", "customer_id", null, "text", "::")]
        [InlineData("SELECT customer_id FROM customers WHERE CAST(customer_id AS text) = '123';", "customer_id", null, "text", "CAST")]
        [InlineData("SELECT c.customer_id FROM customers c WHERE c.customer_id::text = '123';", "customer_id", "c", "text", "::")]
        [InlineData("SELECT customer_id FROM public.customers WHERE public.customers.customer_id::text = '123';", "customer_id", "public.customers", "text", "::")]
        [InlineData("SELECT customer_id FROM customers WHERE '123' = customer_id::text;", "customer_id", null, "text", "::")]
        public void ParseSummary_CastOnColumn_ReturnsExpectedOccurrence(string sql, string expectedColumnName, string? expectedQualifier, string expectedTypeName, string sourceMarker)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.CastOnColumnOccurrences);

            Assert.Equal(expectedColumnName, occurrence.ColumnName);
            Assert.Equal(expectedQualifier, occurrence.Qualifier);
            Assert.Equal(expectedTypeName, occurrence.TargetTypeName);

            Assert.False(string.IsNullOrWhiteSpace(occurrence.Location));

            int expectedOffset = sql.IndexOf(sourceMarker, StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_CastToInteger_ReturnsParserTypeName()
        {
            string sql = "SELECT code FROM customers WHERE CAST(code AS integer) = 123;";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.CastOnColumnOccurrences);

            Assert.Equal("code", occurrence.ColumnName);
            Assert.Equal("pg_catalog.int4", occurrence.TargetTypeName);
        }

        [Fact]
        public void ParseSummary_CastInJoinOn_ReturnsOccurrence()
        {
            string sql = "SELECT c.customer_id FROM customers c JOIN orders o ON c.customer_id::text = o.customer_code;";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.CastOnColumnOccurrences);

            Assert.Equal("customer_id", occurrence.ColumnName);
            Assert.Equal("c", occurrence.Qualifier);
            Assert.Equal("text", occurrence.TargetTypeName);
        }

        [Theory]
        [InlineData("SELECT customer_id FROM customers;")]
        [InlineData("SELECT customer_id::text FROM customers;")]
        [InlineData("SELECT customer_id FROM customers ORDER BY customer_id::text;")]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id = CAST('123' AS integer);")]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id = CAST($1 AS integer);")]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id = CAST(NULL AS integer);")]
        [InlineData("SELECT customer_id FROM customers WHERE (customer_id + 1)::text = '123';")]
        [InlineData("SELECT email FROM customers WHERE lower(email)::text = 'adam';")]
        [InlineData("SELECT customer_id FROM customers GROUP BY customer_id HAVING customer_id::text = '123';")]
        public void ParseSummary_UnsupportedCast_ReturnsNoOccurrences(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.CastOnColumnOccurrences);
        }

        [Fact]
        public void ParseSummary_CastInInnerSelectList_ReturnsNoOccurrences()
        {
            string sql = "SELECT customer_id FROM customers WHERE EXISTS (SELECT customer_id::text FROM orders);";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.CastOnColumnOccurrences);
        }

        [Theory]
        [InlineData("c.total_amount + 100", "+", 100L, true)]
        [InlineData("c.total_amount - 100", "-", 100L, true)]
        [InlineData("2 * c.total_amount", "*", 2L, false)]
        [InlineData("c.total_amount / 2", "/", 2L, true)]
        [InlineData("100 / c.total_amount", "/", 100L, false)]
        public void ParseSummary_ArithmeticOnColumn_ReturnsExpectedOccurrence(string expression, string expectedOperator, long expectedConstant, bool expectedColumnOnLeft)
        {
            string sql = $"SELECT c.order_id FROM orders c WHERE {expression} > 10;";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.ArithmeticOnColumnOccurrences);

            Assert.Equal("total_amount", occurrence.ColumnName);
            Assert.Equal("c", occurrence.Qualifier);
            Assert.Equal(expectedOperator, occurrence.Operator);
            Assert.Equal(expectedConstant, occurrence.ConstantValue);
            Assert.Equal(expectedColumnOnLeft, occurrence.IsColumnOnLeft);

            int expectedOffset = sql.IndexOf(expectedOperator, StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
            Assert.False(string.IsNullOrWhiteSpace(occurrence.Location));
        }

        [Fact]
        public void ParseSummary_ArithmeticInJoinOn_ReturnsOccurrence()
        {
            string sql = "SELECT a.order_id FROM orders a JOIN orders b ON a.total_amount + 100 = b.total_amount;";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.ArithmeticOnColumnOccurrences);

            Assert.Equal("total_amount", occurrence.ColumnName);
            Assert.Equal("a", occurrence.Qualifier);
            Assert.Equal("+", occurrence.Operator);
            Assert.Equal(100L, occurrence.ConstantValue);
            Assert.True(occurrence.IsColumnOnLeft);
        }

        [Theory]
        [InlineData("SELECT total_amount + 100 FROM orders;")]
        [InlineData("SELECT order_id FROM orders WHERE total_amount > 1000 - 100;")]
        [InlineData("SELECT order_id FROM orders WHERE total_amount + $1 > 1000;")]
        [InlineData("SELECT order_id FROM orders WHERE total_amount + tax > 1000;")]
        [InlineData("SELECT order_id FROM orders WHERE total_amount + 1.5 > 1000;")]
        [InlineData("SELECT order_id FROM orders WHERE total_amount % 2 = 0;")]
        public void ParseSummary_UnsupportedArithmetic_ReturnsNoOccurrences(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.ArithmeticOnColumnOccurrences);
        }

        [Theory]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id NOT IN (SELECT customer_id FROM orders);", null)]
        [InlineData("SELECT c.customer_id FROM customers c WHERE c.customer_id NOT IN (SELECT customer_id FROM orders);", "c")]
        public void ParseSummary_NotInSubquery_ReturnsExpectedOccurrence(string sql, string? expectedQualifier)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.NotInSubqueryOccurrences);

            Assert.Equal("customer_id", occurrence.ColumnName);
            Assert.Equal(expectedQualifier, occurrence.Qualifier);

            Assert.False(string.IsNullOrWhiteSpace(occurrence.Location));

            int expectedOffset = sql.IndexOf("NOT IN", StringComparison.Ordinal);

            Assert.Equal((int?)expectedOffset, occurrence.SourceOffset);
        }

        [Theory]
        [InlineData("SELECT c.customer_id FROM customers c JOIN orders o ON c.customer_id NOT IN (SELECT customer_id FROM blocked_customers);")]
        [InlineData("SELECT c.customer_id FROM customers c WHERE c.customer_id NOT IN (SELECT customer_id FROM orders WHERE customer_id IS NOT NULL);")]
        public void ParseSummary_NotInSupportedContext_ReturnsOccurrence(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.NotInSubqueryOccurrences);

            Assert.Equal("customer_id", occurrence.ColumnName);
            Assert.Equal("c", occurrence.Qualifier);
        }

        [Theory]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id IN (SELECT customer_id FROM orders);")]
        [InlineData("SELECT customer_id FROM customers WHERE NOT (customer_id > ANY (SELECT customer_id FROM orders));")]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id NOT IN (1, 2, 3);")]
        [InlineData("SELECT customer_id NOT IN (SELECT customer_id FROM orders) FROM customers;")]
        [InlineData("SELECT customer_id FROM customers WHERE (customer_id + 1) NOT IN (SELECT customer_id FROM orders);")]
        public void ParseSummary_UnsupportedNotIn_ReturnsNoOccurrences(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            var statement = Assert.Single(summary.Statements);

            Assert.Empty(statement.NotInSubqueryOccurrences);
        }
    }      
}
