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

        /// <summary>Az egymásra építhető átírásokat egy közös változatba gyűjti. Minden lépés az előző eredményét vizsgálja újra.</summary>
        public SqlRewriteSuggestion? SuggestCombined(string sql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            var steps = new List<SqlRewriteSuggestion>();
            var visited = new HashSet<string>(StringComparer.Ordinal) { sql };
            string current = sql;
            while (true)
            {
                var next = _rules.SelectMany(rule => rule.Suggest(current))
                    .FirstOrDefault(s => s.SuggestedSql != current);
                if (next is null) break;
                if (next.OriginalSql != current || next.AppliedSteps.Count != 0 || string.IsNullOrWhiteSpace(next.SuggestedSql))
                    throw new InvalidOperationException("Érvénytelen átírási lépés; nem készült közös javaslat.");
                if (!visited.Add(next.SuggestedSql))
                    throw new InvalidOperationException("Az átírások ismétlődő állapothoz vezettek; nem készült közös javaslat.");
                if (steps.Count == 256)
                    throw new InvalidOperationException("Túl sok átírási lépés; bontsd kisebb részekre a lekérdezést.");
                steps.Add(next);
                current = next.SuggestedSql;
            }
            if (steps.Count == 0) return null;
            if (steps.Count == 1) return steps[0];
            return new SqlRewriteSuggestion
            {
                RuleId = SqlRewriteSuggestion.CombinedRuleId,
                Title = $"Összevont SQL-javaslat – {steps.Count} módosítás",
                OriginalSql = sql, SuggestedSql = current,
                AppliedSteps = steps.AsReadOnly(),
                Explanation = string.Join("\n\n", steps.Select((step, i) => $"{i + 1}. {step.Title}\n{step.Explanation}")),
                Preconditions = string.Join("\n\n", steps.Select(s => s.Preconditions).Distinct())
            };
        }

        public IReadOnlyList<SqlRewriteSuggestion> SuggestCombinedStatement(string document, SqlStatementSummary statement)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(statement);
            if (statement.TextStart is not int start || start < 0 || start > document.Length ||
                statement.TextLength <= 0 || statement.TextLength > document.Length - start) return [];
            var combined = SuggestCombined(document.Substring(start, statement.TextLength));
            return combined is null ? [] : new[] { combined };
        }

        public IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            return _rules.SelectMany(rule => rule.Suggest(sql)).ToArray();
        }
    }
}
