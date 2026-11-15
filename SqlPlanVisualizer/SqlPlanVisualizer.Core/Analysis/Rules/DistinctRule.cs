using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class DistinctRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if(statement is null)
                throw new ArgumentNullException(nameof(statement));

            List<SqlAnalysisFinding> findings = new List<SqlAnalysisFinding>();

            foreach (var distinctOccurrence in statement.DistinctOccurrences)
            {
                if (distinctOccurrence.IsDistinctOn)
                    continue;

                string recommendation = distinctOccurrence.HasJoin ? "Nézd meg, hogy a JOIN a vártnál több egyező sort kapcsol-e össze. Ha csak kapcsolódó sorok létezését vizsgálod, egy " +
                    "EXISTS feltétel is szóba jöhet. A DISTINCT-et csak akkor hagyd el, ha az eredmény egyedisége továbbra is biztosított, " +
                    "vagy az ismétlődések megengedettek."
                    : "Ellenőrizd, szükséges-e az eredménysorok egyedisége. Ha igen, a DISTINCT indokolt lehet. Csak akkor hagyd el, ha az " +
                        "ismétlődések megengedettek, vagy az eredmény már nélküle is biztosan egyedi. A feldolgozás költségét a tervben " +
                        "ellenőrizd.";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "DISTINCT_USAGE",
                    Title = "Ismétlődő eredménysorok eltávolítása",
                    Description = "A SELECT DISTINCT minden azonos eredménysorból csak egyet tart meg. Az egyediség biztosítása további rendezést vagy " +
                        $"hash alapú feldolgozást igényelhet. Hely: {distinctOccurrence.Location}.",
                    Recommendation = recommendation,
                    SourceOffset = distinctOccurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
