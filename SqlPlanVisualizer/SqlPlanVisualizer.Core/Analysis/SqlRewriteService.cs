using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    /// <summary>Összegyűjti a regisztrált átírások javaslatait. Nem futtat SQL-t.</summary>
    public sealed class SqlRewriteService
    {
        private readonly IReadOnlyList<ISqlRewriteRule> _rules;

        public SqlRewriteService(IEnumerable<ISqlRewriteRule> rules)
        {
            ArgumentNullException.ThrowIfNull(rules);
            _rules = rules.ToArray();
        }

        /// <summary>A kiválasztott utasítás szövegét emeli ki az elemzett dokumentumból.</summary>
        public IReadOnlyList<SqlRewriteSuggestion> SuggestStatement(string document, SqlStatementSummary statement)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(statement);
            if (statement.TextStart is not int start || start < 0 || start > document.Length ||
                statement.TextLength <= 0 || statement.TextLength > document.Length - start)
                return Array.Empty<SqlRewriteSuggestion>();
            return Suggest(document.Substring(start, statement.TextLength));
        }

        public IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            return _rules.SelectMany(rule => rule.Suggest(sql)).ToArray();
        }
    }
}
