using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class PostgreSqlQueryParser
    {
        public ParseResult Parse(string sql) {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Az sql query nem lehet üres", nameof(sql));

            var result = Parser.Parse(sql);

            if (result.Error != null)
            {
                throw new FormatException(result.Error.Message);
            }

            return result.Value;
        }
    }
}
