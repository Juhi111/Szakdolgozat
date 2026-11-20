using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests
{
    public class ExplainQueryValidatorTests
    {
        [Theory]
        [InlineData("SELECT customer_id FROM customers;")]
        [InlineData("WITH selected AS (SELECT customer_id FROM customers) SELECT * FROM selected;")]
        [InlineData("SELECT ';' AS text; -- A pontosvessző itt nem külön utasítás")]
        public void Validate_SupportedSelect_DoesNotThrow(string sql)
        {
            var validator = new ExplainQueryValidator();

            validator.Validate(sql);
        }

        [Theory]
        [InlineData("SELECT 1; SELECT 2;")]
        [InlineData("DELETE FROM customers;")]
        [InlineData("SELECT * INTO customers_copy FROM customers;")]
        [InlineData("SELECT * FROM customers FOR UPDATE;")]
        [InlineData("WITH deleted AS (DELETE FROM customers RETURNING *) SELECT * FROM deleted;")]
        [InlineData("SELECT * FROM (SELECT * FROM customers FOR UPDATE) AS locked;")]
        public void Validate_UnsupportedStatement_ThrowsArgumentException(string sql)
        {
            var validator = new ExplainQueryValidator();

            Assert.Throws<ArgumentException>(() => validator.Validate(sql));
        }

        [Fact]
        public void Validate_InvalidSql_ThrowsFormatException()
        {
            var validator = new ExplainQueryValidator();

            Assert.Throws<FormatException>(() => validator.Validate("SELECT FROM;"));
        }
    }
}