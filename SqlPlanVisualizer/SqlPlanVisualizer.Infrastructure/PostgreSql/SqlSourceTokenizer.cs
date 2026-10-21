namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class SqlSourceTokenizer
    {
        public List<SqlSourceToken> Tokenize(string sql)
        {
            if (sql is null)
                throw new ArgumentNullException(nameof(sql));

            var tokens = new List<SqlSourceToken>();
            int position = 0;

            while (position < sql.Length)
            {
                // Whitespace kihagyása.
                if (char.IsWhiteSpace(sql[position]))
                {
                    position++;
                    continue;
                }

                // Sorvégi komment kihagyása.
                if (sql[position] == '-' &&
                    position + 1 < sql.Length &&
                    sql[position + 1] == '-')
                {
                    position += 2;

                    while (position < sql.Length &&
                           sql[position] != '\r' &&
                           sql[position] != '\n')
                    {
                        position++;
                    }

                    continue;
                }

                // Blokkkomment kihagyása, beágyazott kommentekkel együtt.
                if (sql[position] == '/' &&
                    position + 1 < sql.Length &&
                    sql[position + 1] == '*')
                {
                    int commentStart = position;
                    int depth = 1;
                    position += 2;

                    while (position < sql.Length && depth > 0)
                    {
                        if (position + 1 < sql.Length &&
                            sql[position] == '/' &&
                            sql[position + 1] == '*')
                        {
                            depth++;
                            position += 2;
                        }
                        else if (position + 1 < sql.Length &&
                                 sql[position] == '*' &&
                                 sql[position + 1] == '/')
                        {
                            depth--;
                            position += 2;
                        }
                        else
                        {
                            position++;
                        }
                    }

                    if (depth > 0)
                    {
                        throw new FormatException(
                            $"Lezáratlan blokkkomment a(z) {commentStart}. karakterpozíción.");
                    }

                    continue;
                }

                // Szövegliterál vagy idézett azonosító.
                if (sql[position] == '\'' || sql[position] == '"')
                {
                    tokens.Add(ReadQuotedToken(sql, ref position));
                    continue;
                }

                // Szavak és nem idézett azonosítók.
                if (char.IsLetter(sql[position]) || sql[position] == '_')
                {
                    int start = position;
                    position++;

                    while (position < sql.Length &&
                           (char.IsLetterOrDigit(sql[position]) ||
                            sql[position] == '_' ||
                            sql[position] == '$'))
                    {
                        position++;
                    }

                    int length = position - start;
                    string text = sql.Substring(start, length);

                    tokens.Add(new SqlSourceToken(text, start, length));
                    continue;
                }

                // Egyéb karakterek egyelőre külön tokenként.
                tokens.Add(new SqlSourceToken(
                    sql[position].ToString(),
                    position,
                    1));

                position++;
            }

            return tokens;
        }

        private SqlSourceToken ReadQuotedToken(string sql, ref int position)
        {
            int start = position;
            char quote = sql[position];
            position++;

            while (position < sql.Length)
            {
                if (sql[position] != quote)
                {
                    position++;
                    continue;
                }
                
                if (position + 1 < sql.Length &&
                    sql[position + 1] == quote)
                {
                    position += 2;
                    continue;
                }
                
                position++;

                int length = position - start;

                return new SqlSourceToken(
                    sql.Substring(start, length),
                    start,
                    length);
            }

            string tokenType = quote == '\''
                ? "szövegliterál"
                : "idézett azonosító";

            throw new FormatException(
                $"Lezáratlan {tokenType} a(z) {start}. karakterpozíción.");
        }
    }
}
