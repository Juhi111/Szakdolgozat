using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class RandomOrderingRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.RandomOrderings)
            {
                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "RANDOM_ORDERING",
                    Title = "Véletlen sorrend előállítása random() használatával",
                    Description = "Az ORDER BY random() véletlen rendezési értékeket állít elő. Sok sornál ezek kiszámítása és rendezése költséges lehet, " +
                        $"és a LIMIT sem garantálja, hogy csak a visszaadott sorokat kell feldolgozni. Hely: {occurrence.Location}.",
                    Recommendation = "Ellenőrizd a rendezés bemenetét és futási idejét. Ha csak mintát szeretnél, vizsgáld meg a TABLESAMPLE vagy más " +
                        "mintavételi eljárás lehetőségét. Ezek eltérő elemszámot, eloszlást vagy szűrési viselkedést adhatnak, ezért nem " +
                        "automatikus helyettesítők. Kis bemenetnél vagy a teljes eredmény véletlen sorrendjéhez a jelenlegi megoldás megfelelő " +
                        "lehet.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
