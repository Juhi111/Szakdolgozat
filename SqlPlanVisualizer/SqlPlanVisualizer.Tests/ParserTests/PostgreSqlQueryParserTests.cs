using SqlPlanVisualizer.Infrastructure.PostgreSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class PostgreSqlQueryParserTests
    {
        [Fact]
        public void Parse_ValidSql_ReturnsSingleStatement() {
            
            string sql = "SELECT customer_id FROM customers;";
            PostgreSqlQueryParser parser = new();
            var result = parser.Parse(sql);
            Assert.NotNull(result);
            Assert.Single(result.Stmts);
        }

        [Fact]
        public void Parse_InvalidSql_ThrowsFormatException(){

            string sql = "SELEC customer_id FROM customers;";
            PostgreSqlQueryParser parser = new();            
            Assert.Throws<FormatException>(() => parser.Parse(sql));
        }
    }
}

