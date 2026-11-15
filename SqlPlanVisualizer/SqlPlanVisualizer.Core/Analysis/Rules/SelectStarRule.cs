using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class SelectStarRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement) {
            if (statement is null)
            {
                throw new ArgumentNullException(nameof(statement));
            }

            List<SqlAnalysisFinding> findings = new();

            foreach (var wildcardProjection in statement.WildcardProjections)
            {
                string projection = wildcardProjection.Qualifier is null ? "*" : $"{wildcardProjection.Qualifier}.*";

                findings.Add(new SqlAnalysisFinding
                {
                    SourceOffset = wildcardProjection.SourceOffset,
                    StatementIndex = statement.StatementIndex,
                    RuleId = "SELECT_STAR",
                    Title = "Minden oszlop lekérése",
                    Description = $"A(z) {projection} kiválasztás minden érintett oszlopot visszaad. Ha ezek egy részére nincs szükség, a lekérdezés " +
                        $"felesleges adatot is továbbíthat. Hely: {wildcardProjection.Location}.",
                    Recommendation = "Sorold fel a ténylegesen használt oszlopokat. Ettől egyértelműbb lesz a lekérdezés, és egy új táblaoszlop nem " +
                        "változtatja meg automatikusan az eredmény szerkezetét. Ha minden oszlopra szükség van, a csillag használata önmagában " +
                        "nem teljesítményhiba."
                });
            }
            return findings;
        }
    }
}
