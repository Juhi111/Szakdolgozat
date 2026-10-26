using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers
{
    /// <summary>
    /// A PgSqlParser segítségével feldolgozza az SQL-szöveget,
    /// és a parser által jelzett hibát kivétellé alakítja.
    /// </summary>
    public class PostgreSqlQueryParser
    {
        /// <summary>
        /// Előállítja az SQL-szöveg utasításainak absztrakt szintaxisfáját.
        /// </summary>
        /// <param name="sql">A feldolgozandó PostgreSQL SQL-szöveg.</param>
        /// <returns>A feldolgozott utasítások AST-jét tartalmazó eredmény.</returns>
        /// <exception cref="ArgumentException">Az SQL-szöveg null, üres vagy csak whitespace karaktereket tartalmaz.</exception>
        /// <exception cref="FormatException">A parser hibát jelez az SQL feldolgozásakor.</exception>
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
