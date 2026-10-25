using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "COUNT(*) használata létezésvizsgálatra",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        "A skalár allekérdezés COUNT(*) eredményét 0-nál nagyobb értékre vizsgálod. " +
                        "Ebben a szerkezetben a feltételnek csak az számít, hogy létezik-e legalább egy megfelelő sor, " +
                        "a pontos darabszámra nincs szüksége.",
                    Recommendation =
                        "Mérlegeld az EXISTS használatát ugyanazzal a FROM résszel és szűrési feltételekkel. " +
                        "Az EXISTS lehetővé teheti, hogy a végrehajtás az első megfelelő sor megtalálásakor befejeződjön. " +
                        "Őrizd meg a külső lekérdezésre hivatkozó kapcsolási feltételeket is. " +
                        "Ne pusztán EXISTS(SELECT COUNT(*) ...) alakra cseréld a kifejezést: " +
                        "az aggregáló SELECT üres bemenetnél is visszaad egy sort. " +
                        "Az átírás előtt ellenőrizd az illékony vagy mellékhatásos függvényeket, " +
                        "mert megváltozhat, hányszor értékelődnek ki. " +
                        "A tényleges teljesítményt EXPLAIN (ANALYZE, BUFFERS) segítségével hasonlítsd össze.",
                    SourceOffset = occurrence.SourceOffset
                });
            }

            return findings;
        }
    }
}