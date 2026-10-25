using PgSqlParser;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class AstContextResolverTests
    {
        [Theory]
        [InlineData("SELECT lower(email) FROM customers;", AstClause.SelectList)]
        [InlineData("SELECT email FROM customers WHERE lower(email) = 'test';", AstClause.Where)]
        [InlineData("SELECT a.id FROM customers a JOIN customers b ON lower(a.email) = b.email;",AstClause.JoinOn)]
        [InlineData("SELECT customer_id FROM customers WHERE customer_id IN (SELECT lower(email) FROM orders);", AstClause.SelectList)]
        public void GetClause_FunctionCall_ReturnsNearestSelectClause(string sql, AstClause expectedClause)
        {
            var parsed = new PostgreSqlQueryParser().Parse(sql);
            var statement = Assert.Single(parsed.Stmts);

            var walker = new PostgreSqlAstWalker();
            var resolver = new AstContextResolver();

            var actualClauses = new List<AstClause>();

            walker.Walk(statement.Stmt, 1, entry =>
            {
                if (entry.Node is FuncCall)
                {
                    actualClauses.Add(resolver.GetClause(entry.Context));
                }
            });

            var actualClause = Assert.Single(actualClauses);

            Assert.Equal(expectedClause, actualClause);
        }
    }
}