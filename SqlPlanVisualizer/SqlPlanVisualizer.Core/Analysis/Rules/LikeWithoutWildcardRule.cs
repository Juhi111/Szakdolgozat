using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class LikeWithoutWildcardRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.LikeOccurrences)
            {
                if (occurrence.HasWildcard)
                    continue;

                string operatorName = occurrence.IsCaseInsensitive
                    ? "ILIKE"
                    : "LIKE";

                string recommendation = occurrence.IsCaseInsensitive
                    ? "Ellenőrizd, hogy teljes szövegegyezést szeretnél-e "
                      + "vizsgálni kis- és nagybetűk megkülönböztetése nélkül. "
                      + "Az ILIKE nem cserélhető automatikusan = operátorra, "
                      + "mert az összehasonlítás jelentése megváltozhat."
                    : "Ha pontos egyezés a cél, vizsgáld meg, hogy "
                      + "használható-e = operátor. Az átírásnál vedd "
                      + "figyelembe az adattípust, a kollációt és az "
                      + "escape-kezelést. Az escape-elt mintaszöveget "
                      + "ne másold át változtatás nélkül.";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "LIKE_WITHOUT_WILDCARD",
                    Title = $"{operatorName} helyettesítő karakter nélkül",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A {operatorName} mintája "
                        + $"('{occurrence.Pattern}') nem tartalmaz "
                        + "aktív % vagy _ helyettesítő karaktert. "
                        + "Ez önmagában nem jelent hibát vagy lassulást.",

                    Recommendation = recommendation,
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}