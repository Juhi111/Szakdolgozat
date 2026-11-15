using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class LeadingWildcardSearchRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.LikeOccurrences)
            {
                if (!occurrence.HasLeadingWildcard)
                    continue;

                string operatorName = occurrence.IsCaseInsensitive ? "ILIKE" : "LIKE";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "LEADING_WILDCARD_SEARCH",
                    Title = "A keresési minta helyettesítő karakterrel kezdődik",

                    Description = $"A {operatorName} mintája ('{occurrence.Pattern}') % vagy _ helyettesítő karakterrel kezdődik. Emiatt nincs rögzített " +
                        $"kezdőszöveg, amely alapján egy hagyományos B-tree index szűkíthetné a keresést. Hely: {occurrence.Location}.",

                    Recommendation = "Ellenőrizd a keresés futási idejét és a feldolgozott sorok számát. Gyakori részszövegkereséshez megfontolható a pg_trgm " +
                        "bővítmény GIN vagy GiST indexe; az előny a mintától is függ. A kezdő helyettesítő karaktert csak akkor hagyd el, ha " +
                        "valóban elegendő a szöveg elején keresni, mert ez megváltoztatja a találatokat.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
