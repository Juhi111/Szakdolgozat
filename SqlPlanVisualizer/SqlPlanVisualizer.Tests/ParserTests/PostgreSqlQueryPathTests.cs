using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class PostgreSqlQueryPathTests
    {
        [Theory]
        [InlineData("SELECT o.* FROM orders o;", "select_stmt", "o")]
        [InlineData("SELECT order_id FROM orders WHERE order_id = (SELECT a.* FROM archived_orders a LIMIT 1);", "select_stmt / where_clause / a_expr / rexpr / sub_link / subselect / select_stmt", "a")]
        [InlineData("SELECT order_id FROM orders WHERE (SELECT a.* FROM archived_orders a LIMIT 1) = order_id;", "select_stmt / where_clause / a_expr / lexpr / sub_link / subselect / select_stmt", "a")]
        [InlineData("SELECT order_id FROM orders ORDER BY order_id, (SELECT a.* FROM archived_orders a LIMIT 1);", "select_stmt / sort_clause[1] / sort_by / node / sub_link / subselect / select_stmt", "a")]
        [InlineData("SELECT COUNT(*) FILTER (WHERE order_id > (SELECT a.* FROM archived_limits a LIMIT 1)) FROM orders;", "select_stmt / target_list[0] / res_target / val / func_call / agg_filter / a_expr / rexpr / sub_link / subselect / select_stmt", "a")]
        public void ParseSummary_Wildcard_ReturnsTechnicalPath(string sql, string expectedPath, string expectedQualifier)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);
            Assert.True(summary.IsSuccess, summary.ErrorMessage);
            var statement = Assert.Single(summary.Statements);
            var occurrence = Assert.Single(statement.WildcardProjections);
            Assert.Equal(expectedQualifier, occurrence.Qualifier);
            Assert.Equal(expectedPath, occurrence.Location);
        }

        [Theory]
        [InlineData("SELECT o.* FROM orders o UNION ALL SELECT a.* FROM archived_orders a;", "o", "select_stmt / larg", "a", "select_stmt / rarg")]
        [InlineData("SELECT order_id FROM orders WHERE order_id IN (SELECT a.* FROM archived_orders a) OR order_id IN (SELECT p.* FROM pending_orders p);", "a", "select_stmt / where_clause / bool_expr / args[0] / sub_link / subselect / select_stmt", "p", "select_stmt / where_clause / bool_expr / args[1] / sub_link / subselect / select_stmt")]
        [InlineData("SELECT power((SELECT a.* FROM archived_orders a LIMIT 1), (SELECT p.* FROM pending_orders p LIMIT 1));", "a", "select_stmt / target_list[0] / res_target / val / func_call / args[0] / sub_link / subselect / select_stmt", "p", "select_stmt / target_list[0] / res_target / val / func_call / args[1] / sub_link / subselect / select_stmt")]
        public void ParseSummary_TwoWildcards_ReturnsDistinctBranchPaths(string sql, string firstQualifier, string firstPath, string secondQualifier, string secondPath)
        {
            var summary = new PostgreSqlQueryService().ParseSummary(sql);
            Assert.True(summary.IsSuccess, summary.ErrorMessage);
            var statement = Assert.Single(summary.Statements);
            Assert.Equal(2, statement.WildcardProjections.Count);

            var first = Assert.Single(statement.WildcardProjections,
                occurrence => occurrence.Qualifier == firstQualifier);
            var second = Assert.Single(statement.WildcardProjections,
                occurrence => occurrence.Qualifier == secondQualifier);

            Assert.Equal(firstPath, first.Location);
            Assert.Equal(secondPath, second.Location);
        }
    }
}
