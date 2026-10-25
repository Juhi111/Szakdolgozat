using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "CASE által választott oszlop szűrésben",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        $"A CASE a(z) ${occurrence.SelectorParameterNumber} paraméter alapján " +
                        $"a(z) {occurrence.ThenColumn} vagy {occurrence.ElseColumn} oszlopot választja, " +
                        $"majd a kiválasztott értéket a(z) ${occurrence.ComparisonParameterNumber} paraméterrel hasonlítja össze. " +
                        "Az eltérő ágakhoz eltérő végrehajtási terv lehet kedvező.",
                    Recommendation =
                        "Vizsgáld meg a végrehajtási tervet mindkét ágat kiválasztó paraméterértékkel, " +
                        "az alkalmazás tényleges paraméterezési módja mellett. " +
                        "Ha mérhető teljesítményprobléma jelentkezik, mérlegeld a két keresési eset " +
                        "külön, paraméterezett lekérdezéssel történő megvalósítását. " +
                        "Őrizd meg a CASE ágkiválasztásának és a NULL-értékek kezelésének jelentését: " +
                        "a WHEN feltétel hamis vagy ismeretlen eredménye esetén az ELSE ág érvényesül. " +
                        "A paraméterértékeket ne fűzd az SQL szövegébe. " +
                        "Ha a meglévő terv megfelelő, nem szükséges átírás.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}