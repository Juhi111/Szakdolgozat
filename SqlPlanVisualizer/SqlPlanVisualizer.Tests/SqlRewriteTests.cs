using PgSqlParser;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests
{
    public class SqlRewriteTests
    {
        private readonly ArithmeticEqualityRewriteRule _rule = new();

        [Theory]
        [InlineData("customer_id + 1 = 101", "customer_id = 100")]
        [InlineData("customer_id - 2 = 98", "customer_id = 100")]
        [InlineData("customer_id + 2 = 1", "customer_id = -1")]
        [InlineData("customer_id + -2 = -3", "customer_id = -1")]
        public void Suggest_ArithmeticEquality_ProducesParseableIndependentCandidate(string condition, string expected)
        {
            string sql = $"SELECT order_id FROM szakdoga_lab.orders WHERE {condition};";
            var suggestion = Assert.Single(_rule.Suggest(sql));
            Assert.Equal(sql, suggestion.OriginalSql);
            Assert.Contains(expected, suggestion.SuggestedSql);
            Assert.Null(Parser.Parse(suggestion.SuggestedSql).Error);
            Assert.Contains("túlcsordulást", suggestion.Preconditions);
            Assert.Contains("nincs igazolva", suggestion.VerificationStatus);
        }

        [Theory]
        [InlineData("SELECT * FROM orders WHERE customer_id / 2 = 100")]
        [InlineData("SELECT * FROM orders WHERE customer_id * 2 = 100")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1 > 100")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1 = $1")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1.5 = 100")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1 = NULL")]
        [InlineData("SELECT * FROM orders WHERE 1 + customer_id = 100")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1 = 100 OR customer_id = 2")]
        [InlineData("SELECT * FROM orders WHERE NOT (customer_id + 1 = 100)")]
        [InlineData("SELECT customer_id + 1 = 100 FROM orders")]
        [InlineData("SELECT * FROM orders WHERE customer_id - 1 = 2147483647")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1 = 2147483648")]
        [InlineData("SELECT * FROM orders WHERE customer_id::int + 1 = 100")]
        [InlineData("SELECT * FROM orders WHERE customer_id OPERATOR(public.+) 1 = 100")]
        [InlineData("SELECT * FROM orders o JOIN customers c ON o.customer_id = c.customer_id WHERE o.customer_id + 1 = 100")]
        [InlineData("WITH x AS (SELECT * FROM orders) SELECT * FROM x WHERE customer_id + 1 = 100")]
        [InlineData("SELECT * INTO copy FROM orders WHERE customer_id + 1 = 100")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1 = 100 FOR UPDATE")]
        [InlineData("SELECT * FROM orders WHERE customer_id + 1 = 100; SELECT 1")]
        [InlineData("DELETE FROM orders WHERE customer_id + 1 = 100")]
        public void Suggest_UnsupportedShape_DoesNotOfferRewrite(string sql) => Assert.Empty(_rule.Suggest(sql));

        [Fact]
        public void Suggest_MultiplePredicates_ChangesOnlyOnePerCandidateAndKeepsQuotedNames()
        {
            const string sql = "-- Árvíztűrő\r\nSELECT 'a + 1 = 3' AS note FROM orders AS \"Ő\" WHERE \"Ő\".\"Azonosító\" + 1 = 101 AND other - 2 = 98 ORDER BY other LIMIT 5";
            var suggestions = _rule.Suggest(sql);
            Assert.Equal(2, suggestions.Count);
            Assert.Contains("\"Ő\".\"Azonosító\" = 100", suggestions[0].SuggestedSql);
            var firstWhere = Parser.Parse(suggestions[0].SuggestedSql).Value!.Stmts[0].Stmt.SelectStmt.WhereClause.BoolExpr;
            Assert.Equal("-", firstWhere.Args[1].AExpr.Lexpr.AExpr.Name[0].String.Sval);
            Assert.Equal(98, firstWhere.Args[1].AExpr.Rexpr.AConst.Ival.Ival);
            var secondWhere = Parser.Parse(suggestions[1].SuggestedSql).Value!.Stmts[0].Stmt.SelectStmt.WhereClause.BoolExpr;
            Assert.Equal("+", secondWhere.Args[0].AExpr.Lexpr.AExpr.Name[0].String.Sval);
            Assert.Equal(101, secondWhere.Args[0].AExpr.Rexpr.AConst.Ival.Ival);
            Assert.Contains("other = 100", suggestions[1].SuggestedSql);
            foreach (var suggestion in suggestions)
            {
                Assert.Equal(sql, suggestion.OriginalSql);
                Assert.Contains("'a + 1 = 3'", suggestion.SuggestedSql);
                Assert.Contains("ORDER BY other LIMIT 5", suggestion.SuggestedSql);
            }
        }

        [Fact]
        public void SuggestStatement_SelectsOnlyTheRequestedStatementFromUnicodeDocument()
        {
            const string document = "-- Első: őű\r\nSELECT * FROM orders WHERE customer_id + 1 = 11;\r\nSELECT * FROM orders WHERE customer_id - 1 = 99;";
            var summary = new PostgreSqlQueryService().ParseSummary(document);
            Assert.True(summary.IsSuccess);
            var service = new SqlRewriteService(new ISqlRewriteRule[] { _rule });
            var suggestion = Assert.Single(service.SuggestStatement(document, summary.Statements[1]));
            Assert.Contains("customer_id = 100", suggestion.SuggestedSql);
            Assert.DoesNotContain("= 11", suggestion.OriginalSql);
            summary.Statements[1].TextLength = int.MaxValue;
            Assert.Empty(service.SuggestStatement(document, summary.Statements[1]));
        }
    }
}
