using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "Lehetséges hiányzó kapcsolási feltétel",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        $"A FROM-listában több, vesszővel elválasztott táblahivatkozás szerepel: {relations}. " +
                        "A SELECT nem tartalmaz WHERE vagy HAVING feltételt. " +
                        "A FROM eredménye a források Descartes-szorzata: " +
                        "minden forrás minden sora kombinálódik a többi forrás soraival. " +
                        "A későbbi aggregálás vagy eredménykorlátozás ettől eltérő végső sorszámot adhat.",
                    Recommendation =
                        "Ellenőrizd, hogy szándékosan van-e szükség minden sorkombinációra. " +
                        "Ha kapcsolódó rekordokat szeretnél összerendelni, adj meg megfelelő " +
                        "JOIN ... ON vagy JOIN ... USING kapcsolatot az üzleti jelentés alapján. " +
                        "A kapcsolási kulcsot az elemző nem tudja meghatározni. " +
                        "Ha a Descartes-szorzat szándékos, az explicit CROSS JOIN világosabban fejezi ki a célt, " +
                        "de önmagában nem csökkenti a végrehajtás költségét. " +
                        "A tényleges sorszámokat és költséget a végrehajtási terv alapján ellenőrizd.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}