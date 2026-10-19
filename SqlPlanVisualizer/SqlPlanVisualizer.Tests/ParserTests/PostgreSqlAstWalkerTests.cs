using SqlPlanVisualizer.Infrastructure.PostgreSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class PostgreSqlAstWalkerTests
    {
        [Fact]
        public void Walk_SelectWildcard_VisitsRootAndDescendants()
        {
            var parser = new PostgreSqlQueryParser();
            var root = parser.Parse("SELECT * FROM orders;");
            var walker = new PostgreSqlAstWalker();

            var visited = new List<AstVisitEntry>();

            walker.Walk(root, 1, entry => visited.Add(entry));

            Assert.NotEmpty(visited);
            Assert.Same(root, visited[0].Node);
            Assert.Equal(0, visited[0].Context.Depth);
            Assert.Null(visited[0].Context.Parent);

            Assert.Contains(visited, entry =>
                entry.Node is PgSqlParser.SelectStmt);

            Assert.Contains(visited, entry =>
                entry.Node is PgSqlParser.ColumnRef);

            Assert.All(visited.Skip(1), entry =>
            {
                Assert.NotNull(entry.Context.Parent);
                Assert.Equal(
                    entry.Context.Parent.Depth + 1,
                    entry.Context.Depth);
            });
        }
    }
}
