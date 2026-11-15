using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class OptionalParameterFilterRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(
            SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.OptionalParameterFilters)
            {
                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                string parameter = $"${occurrence.ParameterNumber}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "OPTIONAL_PARAMETER_FILTER",
                    Title = "Ugyanaz a feltétel kapcsolja be és ki a szűrést",

                    Description = $"A(z) {parameter} IS NULL OR {columnReference} = {parameter} minta NULL paraméternél minden sort átenged ezen a " +
                        "részfeltételen, más értéknél pedig az oszlopra szűr. A két esethez eltérő terv lehet kedvező. A felismert alakban az " +
                        $"ágak vagy az összehasonlítás oldalai fel is lehetnek cserélve. Hely: {occurrence.Location}.",

                    Recommendation = "Vizsgáld meg a NULL és a jellemző nem NULL paraméterekkel futó terveket, az alkalmazás tényleges előkészített " +
                        "lekérdezéseivel. Ha a közös terv valamelyik esetben lassú, mérlegeld a szűrt és szűrés nélküli lekérdezés " +
                        "különválasztását. A paramétereket továbbra is külön add át. JOIN ON feltételnél a kapcsolás és a NULL-értékek kezelése " +
                        "is maradjon azonos.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
