using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class AstPathFormatterTests
    {
        [Fact]
        public void Format_NestedContext_ReturnsPathFromRoot()
        {
            var root = new AstVisitContext(1);
            var child = new AstVisitContext(1, root, "target_list", 0);
            var grandchild = new AstVisitContext(1, child, "val");

            var formatter = new AstPathFormatter();

            var result = formatter.Format(grandchild);

            Assert.Equal("target_list[0] / val", result);
        }

        [Fact]
        public void Format_RootContext_ReturnsEmptyPath()
        {
            var formatter = new AstPathFormatter();
            Assert.Equal(string.Empty, formatter.Format(new AstVisitContext(1)));
        }
    }
}
