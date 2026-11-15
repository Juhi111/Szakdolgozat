using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class CountForExistenceRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.CountForExistenceOccurrences)
            {
                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "COUNT_FOR_EXISTENCE",
                    Title = "Sorokat számolunk, pedig csak a létezésük számít",
                    Description = "Az allekérdezés COUNT(*) eredményéről csak azt vizsgáljuk, hogy nagyobb-e nullánál. A feltételhez elegendő tudni, van-e " +
                        $"legalább egy megfelelő sor; a pontos darabszám nem szükséges. Hely: {occurrence.Location}.",
                    Recommendation = "Mérlegeld az EXISTS használatát az eredeti táblák és szűrési feltételek megtartásával. Így a végrehajtás már az első " +
                        "találat után megállhat. A külső lekérdezéshez kapcsoló feltételeket is őrizd meg. Ne EXISTS(SELECT COUNT(*) ...) alakot " +
                        "használj: ez az aggregáló SELECT üres bemenetnél is visszaad egy sort. Változó eredményű vagy mellékhatásos " +
                        "függvényeknél a kiértékelések száma is számíthat. Hasonlítsd össze az eredményt és a futási időt.",
                    SourceOffset = occurrence.SourceOffset
                });
            }

            return findings;
        }
    }
}
