using Xunit;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using System.Text.Json;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class PostgreSqlPlanParserTests
    {
        //helyes Explain JSON tesztelése
        [Fact]
        public void Parse_ValidNestedPlan_ReturnsPlanWithChild() {

            var testJson = "[{\"Plan\":{\"Node Type\": \"Aggregate\",\"Plans\":[{\"Node Type\": \"Seq Scan\", \"Relation Name\": \"orders\"}]}}]";
            var testParser = new PostgreSqlPlanParser();

            var result = testParser.Parse(testJson);
            var explain = Assert.Single(result);
            Assert.NotNull(explain.Plan);
            Assert.Equal("Aggregate", explain.Plan.NodeType);
            Assert.Single(explain.Plan.Plans);
            Assert.Equal("Seq Scan", explain.Plan.Plans[0].NodeType);
            Assert.Equal("orders", explain.Plan.Plans[0].RelationName);
            Assert.Empty(explain.Plan.Plans[0].Plans);
        }

        //helytelen Explain JSON tesztelése, ahol a child node hiányzik a Node Type mezőből
        [Fact]
        public void Parse_ChildWithoutNodeType_ThrowsJsonException() {

            var testJson = "[{\"Plan\": {\"Node Type\": \"Aggregate\",\"Plans\": [{\"Relation Name\": \"orders\"}]}}]";
            var testParser = new PostgreSqlPlanParser();
            Assert.Throws<JsonException>(() => testParser.Parse(testJson));
        }
    }
}
