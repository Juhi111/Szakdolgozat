using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class PostgreSqlQueryServiceTests
    {
        private static SqlStatementSummary ParseSingle(string sql)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);

            return Assert.Single(summary.Statements);
        }

        [Fact]
        public void ParseSummary_Wildcard_SkipsDirectExistsProjection()
        {
            string sql = "SELECT c.* FROM customers c WHERE EXISTS (SELECT * FROM orders);";

            var statement = ParseSingle(sql);
            var occurrence = Assert.Single(statement.WildcardProjections);

            Assert.Equal("SELECT", statement.StatementType);
            Assert.Equal("c", occurrence.Qualifier);
            Assert.Equal((int?)sql.IndexOf("c.*", StringComparison.Ordinal), occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_Distinct_ReturnsOccurrence()
        {
            var statement = ParseSingle("SELECT DISTINCT c.customer_id FROM customers c JOIN orders o ON o.customer_id = c.customer_id;");

            var occurrence = Assert.Single(statement.DistinctOccurrences);

            Assert.False(occurrence.IsDistinctOn);
            Assert.True(occurrence.HasJoin);
        }

        [Fact]
        public void ParseSummary_NullComparison_SkipsIsNull()
        {
            var statement = ParseSingle("SELECT order_id FROM orders WHERE status = NULL OR customer_id IS NULL;");

            var occurrence = Assert.Single(statement.NullComparisons);

            Assert.Equal("=", occurrence.Operator);
        }

        [Fact]
        public void ParseSummary_Limit_ReturnsValueAndOrdering()
        {
            var statement = ParseSingle("SELECT order_id FROM orders ORDER BY order_id LIMIT 20;");

            var occurrence = Assert.Single(statement.LimitOccurrences);

            Assert.Equal(20L, occurrence.RowLimit);
            Assert.True(occurrence.HasOrderBy);
        }

        [Fact]
        public void ParseSummary_Like_HandlesEscape()
        {
            var statement = ParseSingle("SELECT email FROM customers WHERE email ILIKE 'adam!_%' ESCAPE '!';");

            var occurrence = Assert.Single(statement.LikeOccurrences);

            Assert.Equal("adam!_%", occurrence.Pattern);
            Assert.True(occurrence.IsCaseInsensitive);
            Assert.True(occurrence.HasWildcard);
            Assert.False(occurrence.HasLeadingWildcard);
        }

        [Fact]
        public void ParseSummary_FunctionOnColumn_SkipsSelectList()
        {
            var statement = ParseSingle("SELECT upper(c.email) FROM customers c WHERE lower(c.email) = 'adam';");

            var occurrence = Assert.Single(statement.FunctionOnColumnOccurrences);

            Assert.Equal("lower", occurrence.FunctionName);
            Assert.Equal("email", occurrence.ColumnName);
            Assert.Equal("c", occurrence.Qualifier);
        }

        [Fact]
        public void ParseSummary_Casts_RouteToSeparateRules()
        {
            var statement = ParseSingle("SELECT order_id FROM orders WHERE created_at::date = DATE '2026-10-25' AND order_id::text = '123' AND customer_id::bigint = 456;");

            Assert.Equal(3, statement.CastOnColumnOccurrences.Count);

            var dateFinding = Assert.Single(new DateCastInPredicateRule().Analyze(statement));
            var textFinding = Assert.Single(new TextCastInPredicateRule().Analyze(statement));
            var generalFinding = Assert.Single(new CastOnColumnRule().Analyze(statement));

            Assert.Equal("DATE_CAST_IN_PREDICATE", dateFinding.RuleId);
            Assert.Equal("TEXT_CAST_IN_PREDICATE", textFinding.RuleId);
            Assert.Equal("CAST_ON_COLUMN", generalFinding.RuleId);
        }

        [Fact]
        public void ParseSummary_Arithmetic_PreservesOperandOrder()
        {
            var statement = ParseSingle("SELECT order_id FROM orders WHERE 100 / total_amount > 2;");

            var occurrence = Assert.Single(statement.ArithmeticOnColumnOccurrences);

            Assert.Equal("total_amount", occurrence.ColumnName);
            Assert.Equal("/", occurrence.Operator);
            Assert.Equal(100L, occurrence.ConstantValue);
            Assert.False(occurrence.IsColumnOnLeft);
        }

        [Fact]
        public void ParseSummary_NotInSubquery_ReturnsOccurrence()
        {
            var statement = ParseSingle("SELECT c.customer_id FROM customers c WHERE c.customer_id NOT IN (SELECT customer_id FROM orders);");

            var occurrence = Assert.Single(statement.NotInSubqueryOccurrences);

            Assert.Equal("customer_id", occurrence.ColumnName);
            Assert.Equal("c", occurrence.Qualifier);
        }

        [Fact]
        public void ParseSummary_YearExtraction_SkipsOtherDateParts()
        {
            var statement = ParseSingle("SELECT order_id FROM orders WHERE EXTRACT(YEAR FROM created_at) = 2026 AND EXTRACT(MONTH FROM created_at) = 10;");

            var occurrence = Assert.Single(statement.YearExtractionOccurrences);

            Assert.Equal("extract", occurrence.FunctionName);
            Assert.Equal("created_at", occurrence.ColumnName);
            Assert.Equal("=", occurrence.ComparisonOperator);
        }

        [Fact]
        public void ParseSummary_Concatenation_ReturnsOneOccurrenceForChain()
        {
            var statement = ParseSingle("SELECT customer_id FROM customers c WHERE c.first_name || ' ' || c.last_name = 'Adam Kovacs';");

            var occurrence = Assert.Single(statement.ConcatenationOccurrences);

            Assert.Equal(new[] { "c.first_name", "c.last_name" }, occurrence.ColumnReferences);
        }

        [Fact]
        public void ParseSummary_OptionalParameter_RequiresMatchingParameters()
        {
            var statement = ParseSingle("SELECT customer_id FROM customers WHERE ($1 = customer_id OR $1 IS NULL) AND ($2 IS NULL OR customer_id = $3);");

            var occurrence = Assert.Single(statement.OptionalParameterFilters);

            Assert.Equal(1, occurrence.ParameterNumber);
            Assert.Equal("customer_id", occurrence.ColumnName);
        }

        [Fact]
        public void ParseSummary_Case_ReturnsSelectedColumns()
        {
            var statement = ParseSingle("SELECT customer_id FROM customers c WHERE CASE WHEN $1 = 'email' THEN c.email ELSE c.phone END = $2;");

            var occurrence = Assert.Single(statement.CaseInPredicateOccurrences);

            Assert.Equal(1, occurrence.SelectorParameterNumber);
            Assert.Equal(2, occurrence.ComparisonParameterNumber);
            Assert.Equal("c.email", occurrence.ThenColumn);
            Assert.Equal("c.phone", occurrence.ElseColumn);
        }

        [Fact]
        public void ParseSummary_NotInWithNull_ReturnsOneOccurrence()
        {
            string sql = "SELECT customer_id FROM customers WHERE customer_id NOT IN (NULL, 1, NULL);";

            var statement = ParseSingle(sql);
            var occurrence = Assert.Single(statement.NotInWithNullLiterals);

            Assert.Equal((int?)sql.IndexOf("NULL", StringComparison.Ordinal), occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_CountForExistence_SkipsCountColumn()
        {
            var statement = ParseSingle("SELECT customer_id FROM customers WHERE (SELECT COUNT(*) FROM orders) > 0 AND (SELECT COUNT(order_id) FROM orders) > 0;");

            Assert.Single(statement.CountForExistenceOccurrences);
        }

        [Fact]
        public void ParseSummary_Having_CollectsOnlyGroupKeyFilter()
        {
            var statement = ParseSingle("SELECT customer_id, COUNT(*) FROM orders GROUP BY customer_id HAVING customer_id > 100 AND COUNT(*) > 5;");

            var occurrence = Assert.Single(statement.RowFiltersInHaving);

            Assert.Equal("customer_id", occurrence.ColumnName);
            Assert.Equal(">", occurrence.Operator);
            Assert.Equal(100L, occurrence.ConstantValue);
        }

        [Fact]
        public void ParseSummary_CartesianProduct_ReturnsRelations()
        {
            var statement = ParseSingle("SELECT c.customer_id, o.order_id FROM public.customers c, orders o;");

            var occurrence = Assert.Single(statement.PossibleCartesianProducts);

            Assert.Equal(new[] { "public.customers AS c", "orders AS o" }, occurrence.Relations);
        }

        [Fact]
        public void ParseSummary_Offset_ReturnsValueAndPosition()
        {
            string sql = "SELECT order_id FROM orders ORDER BY order_id LIMIT 20 OFFSET 100000;";

            var statement = ParseSingle(sql);
            var occurrence = Assert.Single(statement.OffsetOccurrences);

            Assert.Equal(100000L, occurrence.RowOffset);
            Assert.Equal((int?)sql.IndexOf("100000", StringComparison.Ordinal), occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_Union_SkipsUnionAllNode()
        {
            var statement = ParseSingle("SELECT email FROM customers UNION SELECT email FROM archived_customers UNION ALL SELECT email FROM old_customers;");

            Assert.Single(statement.UnionDeduplications);
        }

        [Fact]
        public void ParseSummary_NaturalJoin_SkipsExplicitJoin()
        {
            var statement = ParseSingle("SELECT c.customer_id FROM customers c NATURAL JOIN orders o JOIN contacts x ON x.customer_id = c.customer_id;");

            Assert.Single(statement.NaturalJoins);
        }

        [Fact]
        public void ParseSummary_RandomOrdering_SkipsSelectListCall()
        {
            string sql = "SELECT order_id, random() FROM orders ORDER BY random();";

            var statement = ParseSingle(sql);
            var occurrence = Assert.Single(statement.RandomOrderings);

            Assert.Equal((int?)sql.LastIndexOf("random()", StringComparison.Ordinal), occurrence.SourceOffset);
        }

        [Fact]
        public void ParseSummary_MultipleStatements_KeepsDataSeparate()
        {
            string sql = "SELECT email FROM customers; SELECT email FROM customers WHERE email LIKE '%adam%';";

            var summary = new PostgreSqlQueryService().ParseSummary(sql);

            Assert.True(summary.IsSuccess, summary.ErrorMessage);
            Assert.Equal(2, summary.Statements.Count);

            Assert.Equal(1, summary.Statements[0].StatementIndex);
            Assert.Empty(summary.Statements[0].LikeOccurrences);

            Assert.Equal(2, summary.Statements[1].StatementIndex);
            var occurrence = Assert.Single(summary.Statements[1].LikeOccurrences);

            Assert.Equal("%adam%", occurrence.Pattern);
            Assert.Equal((int?)sql.IndexOf("LIKE", StringComparison.Ordinal), occurrence.SourceOffset);
        }
    }
}