using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "Keresés kezdő helyettesítő karakterrel",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A {operatorName} mintája "
                        + $"('{occurrence.Pattern}') aktív % vagy _ "
                        + "helyettesítő karakterrel kezdődik. "
                        + "Nincs rögzített kezdőszövege, amely alapján "
                        + "a szokásos B-tree prefixkeresés szűkíthetné "
                        + "a keresési tartományt. "
                        + "Ez lehetséges teljesítményprobléma, "
                        + "önmagában nem bizonyít lassulást.",

                    Recommendation =
                        "Ellenőrizd a tényleges végrehajtást "
                        + "EXPLAIN (ANALYZE, BUFFERS) segítségével. "
                        + "Gyakori részszövegkeresésnél megfontolható "
                        + "pg_trgm alapú GIN vagy GiST index; "
                        + "ennek hatékonysága a mintától is függ. "
                        + "A kezdő helyettesítő karaktert csak akkor "
                        + "hagyd el, ha a keresés kívánt jelentése "
                        + "ezt megengedi.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}