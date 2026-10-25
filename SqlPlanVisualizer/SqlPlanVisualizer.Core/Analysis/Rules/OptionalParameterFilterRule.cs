using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "Opcionális paraméteres szűrés",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A feltétel a(z) {parameter} IS NULL OR "
                        + $"{columnReference} = {parameter} mintát követi, "
                        + "esetleg felcserélt ágakkal vagy operandusokkal. "
                        + "NULL paraméternél ez a részfeltétel minden "
                        + "sorra igaz; egyébként oszlopértékre szűr. "
                        + "A két esethez eltérő végrehajtási stratégia "
                        + "lehet kedvező.",

                    Recommendation =
                        "Ellenőrizd a végrehajtási tervet NULL és "
                        + "jellemző nem NULL paraméterértékekkel is, "
                        + "az alkalmazás tényleges paraméterezési és "
                        + "előkészítettutasítás-használata mellett. "
                        + "Vizsgáld meg, hogy általános vagy az adott "
                        + "paraméterértékhez készített terv fut-e. "
                        + "Ha mérhető probléma jelentkezik, mérlegeld "
                        + "a szűrés nélküli és a szűrt lekérdezés "
                        + "különválasztását, vagy csak az aktív "
                        + "feltételek paraméterezett összeállítását. "
                        + "A paraméterértékeket ne fűzd az SQL szövegébe. "
                        + "JOIN ON feltételnél őrizd meg a kapcsolás "
                        + "jelentését is. Ha a meglévő terv megfelelő, "
                        + "nem szükséges átírás.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}