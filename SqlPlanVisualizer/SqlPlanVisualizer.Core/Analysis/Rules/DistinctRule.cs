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

                string recommendation = distinctOccurrence.HasJoin ? "Ellenőrizd, hogy a JOIN eredményében keletkező ismétlődések miatt használod-e a DISTINCT-et. Vizsgáld meg a kapcsolási feltételeket és a táblák közötti kapcsolatot. A DISTINCT-et csak az eredmény helyességének ellenőrzése után távolítsd el."
                    : "Ellenőrizd, hogy szükséges-e a duplikált eredménysorok eltávolítása. A DISTINCT-et csak az eredmény helyességének ellenőrzése után távolítsd el.";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "DISTINCT_USAGE",
                    Title = "DISTINCT használatának felülvizsgálata",
                    Description = $"Hely: {distinctOccurrence.Location}. A SELECT DISTINCT-et használ.",
                    Recommendation = recommendation,
                    SourceOffset = distinctOccurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
