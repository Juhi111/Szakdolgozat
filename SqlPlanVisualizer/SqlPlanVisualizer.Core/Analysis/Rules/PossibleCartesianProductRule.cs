using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class PossibleCartesianProductRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.PossibleCartesianProducts)
            {
                string relations = string.Join(", ", occurrence.Relations);

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "POSSIBLE_CARTESIAN_PRODUCT",
                    Title = "Hiányozhat a táblák közötti kapcsolási feltétel",
                    Description = $"A FROM részben több, vesszővel elválasztott forrás szerepel: {relations}. A lekérdezéshez nem tartozik WHERE vagy " +
                        "HAVING feltétel, így a források minden sora kombinálódik a többi forrás soraival. Ez nagy köztes eredményt adhat akkor " +
                        $"is, ha a végén kevés sor marad. Hely: {occurrence.Location}.",
                    Recommendation = "Ha összetartozó rekordokat keresel, add meg a kapcsolatot JOIN ... ON vagy JOIN ... USING feltétellel. A megfelelő " +
                        "kapcsolási kulcsot az adatok jelentése alapján válaszd ki. Ha valóban minden sorkombinációra szükség van, a CROSS JOIN " +
                        "egyértelműbbé teszi ezt a szándékot, de nem csökkenti a feldolgozandó adatmennyiséget.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
