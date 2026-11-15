using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class CaseInPredicateRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.CaseInPredicateOccurrences)
            {
                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "CASE_IN_PREDICATE",
                    Title = "Paramétertől függ, melyik oszlopra szűrünk",
                    Description = $"A CASE a(z) ${occurrence.SelectorParameterNumber} paraméter alapján a(z) {occurrence.ThenColumn} vagy " +
                        $"{occurrence.ElseColumn} oszlopot választja, majd annak értékét a(z) ${occurrence.ComparisonParameterNumber} " +
                        $"paraméterrel hasonlítja össze. A két kereséshez eltérő végrehajtási terv lehet kedvező. Hely: {occurrence.Location}.",
                    Recommendation = "Próbáld ki mindkét ágat jellemző paraméterértékekkel, az alkalmazás tényleges paraméterezési módja mellett. Ha lassú a " +
                        "keresés, mérlegeld két külön, paraméterezett lekérdezés használatát. Őrizd meg a NULL-kezelést is: a WHEN feltétel " +
                        "hamis vagy ismeretlen eredménye az ELSE ágat választja. A paramétereket továbbra is külön add át, ne fűzd az SQL " +
                        "szövegébe.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
