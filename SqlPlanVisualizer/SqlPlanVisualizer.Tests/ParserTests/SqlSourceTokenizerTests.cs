using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests.ParserTests
{
    public class SqlSourceTokenizerTests
    {
        [Theory]
        [InlineData("-- DISTINCT\nSELECT DISTINCT customer_id;")]
        [InlineData("/* DISTINCT */ SELECT DISTINCT customer_id;")]
        [InlineData("/* kulso /* DISTINCT */ komment */ SELECT DISTINCT customer_id;")]
        public void Tokenize_Comments_SkipsCommentAndPreservesOffset(string sql)
        {
            var tokenizer = new SqlSourceTokenizer();

            var tokens = tokenizer.Tokenize(sql);

            var distinct = Assert.Single(
                tokens,
                token => token.Text == "DISTINCT");

            Assert.Equal(
                sql.LastIndexOf("DISTINCT", StringComparison.Ordinal),
                distinct.Start);

            Assert.Equal(8, distinct.Length);
        }

        [Fact]
        public void Tokenize_UnclosedBlockComment_ThrowsFormatException()
        {
            var tokenizer = new SqlSourceTokenizer();

            Assert.Throws<FormatException>(
                () => tokenizer.Tokenize("SELECT /* nincs lezarva"));
        }

        [Theory]
        [InlineData("'DISTINCT'")]
        [InlineData("'it''s DISTINCT'")]
        [InlineData("\"DISTINCT\"")]
        [InlineData("\"customer\"\"DISTINCT\"")]
        public void Tokenize_QuotedText_PreservesWholeToken(string quotedText)
        {
            string sql = $"SELECT DISTINCT {quotedText};";

            var tokens = new SqlSourceTokenizer().Tokenize(sql);

            Assert.Collection(
                tokens,
                token => Assert.Equal("SELECT", token.Text),
                token => Assert.Equal("DISTINCT", token.Text),
                token =>
                {
                    Assert.Equal(quotedText, token.Text);
                    Assert.Equal(16, token.Start);
                    Assert.Equal(quotedText.Length, token.Length);
                },
                token => Assert.Equal(";", token.Text));
        }

        [Theory]
        [InlineData("SELECT 'nincs lezarva")]
        [InlineData("SELECT \"nincs lezarva")]
        public void Tokenize_UnclosedQuote_ThrowsFormatException(string sql)
        {
            var tokenizer = new SqlSourceTokenizer();

            Assert.Throws<FormatException>(() => tokenizer.Tokenize(sql));
        }
    }
}