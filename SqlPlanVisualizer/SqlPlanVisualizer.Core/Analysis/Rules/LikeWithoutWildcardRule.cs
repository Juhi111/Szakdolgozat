using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                    ? "Az ILIKE itt teljes szövegegyezést keres. Ellenőrizd, hogy ezt szeretnéd-e "
                      + "vizsgálni kis- és nagybetűk megkülönböztetése nélkül. "
                      + "Az ILIKE nem cserélhető automatikusan = operátorra, "
                      + "mert az összehasonlítás jelentése megváltozhat."
                    : "Pontos egyezéshez olvashatóbb lehet az = feltétel. Előbb ellenőrizd, hogy "
                      + "használható-e = operátor. Az átírásnál vedd "
                      + "figyelembe az adattípust, a szövegek összehasonlítási szabályait (kolláció) és az "
                      + "escape-kezelést. Az escape-elt mintaszöveget "
                      + "ne másold át változtatás nélkül.";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "LIKE_WITHOUT_WILDCARD",
                    Title = $"Teljes szövegegyezés keresése {operatorName} használatával",

                    Description = $"A {operatorName} mintája ('{occurrence.Pattern}') nem tartalmaz aktív % vagy _ helyettesítő karaktert, így nem " +
                        "részszövegmintát keres. Érdemes ellenőrizni, hogy ez fejezi-e ki legjobban a kívánt egyezést. Hely: " +
                        $"{occurrence.Location}.",

                    Recommendation = recommendation,
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
