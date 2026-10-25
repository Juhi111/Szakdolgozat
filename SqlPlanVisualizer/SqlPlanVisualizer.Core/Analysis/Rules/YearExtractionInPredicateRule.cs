using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class YearExtractionInPredicateRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(
            SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.YearExtractionOccurrences)
            {
                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                string expression = occurrence.FunctionName == "extract"
                    ? $"EXTRACT(YEAR FROM {columnReference})"
                    : $"date_part('year', {columnReference})";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "YEAR_EXTRACTION_IN_PREDICATE",
                    Title = "Év kinyerése szűrési feltételben",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A(z) {expression} kifejezés "
                        + $"'{occurrence.ComparisonOperator}' operátorral "
                        + "végzett összehasonlításban szerepel. "
                        + "A feltétel az oszlopból kinyert évet vizsgálja, "
                        + "ami befolyásolhatja az eredeti oszlop indexének "
                        + "felhasználhatóságát. "
                        + "Ez önmagában nem bizonyít lassulást.",

                    Recommendation =
                        "Ha dátumot vagy időbélyeget szűrsz egy adott évre, "
                        + "mérlegeld az eredeti oszlopon megadott, "
                        + "az év kezdetétől a következő év kezdetéig "
                        + "tartó félig nyílt tartományt "
                        + "(>= alsó határ és < felső határ). "
                        + "Más összehasonlító operátornál a megfelelő "
                        + "határokat és az operandusok sorrendjét "
                        + "külön kell figyelembe venni; az év kizárása "
                        + "például nem egyetlen ilyen tartomány. "
                        + "Az oszlop eredeti típusát, az időzónát és "
                        + "a másik operandus jelentését is ellenőrizd. "
                        + "Intervallum típusú értékre az év szerinti "
                        + "dátumtartományos javaslat nem alkalmazható. "
                        + "Az átírás nem automatikus. "
                        + "A tényleges hatást EXPLAIN (ANALYZE, BUFFERS) "
                        + "segítségével vizsgáld meg.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}