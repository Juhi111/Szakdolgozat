using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                    Title = "Az oszlopból kinyert évre szűrünk",

                    Description = $"A feltétel a(z) {expression} kifejezést használja a(z) {occurrence.ComparisonOperator} összehasonlításban. Így az oszlop " +
                        "teljes értéke helyett annak év részét vizsgálja, ami befolyásolhatja az eredeti oszlop indexének használatát. Hely: " +
                        $"{occurrence.Location}.",

                    Recommendation = "Ha dátumokat vagy időbélyegeket keresel egy adott évre, vizsgáld meg az év kezdetétől a következő év kezdetéig tartó " +
                        "feltételt (>= alsó határ és < felső határ). Más operátornál és fordított összehasonlításnál a megfelelő határok " +
                        "eltérnek; egy év kizárása például nem egyetlen ilyen tartomány. Ellenőrizd a típust, az időzónát és a másik operandust " +
                        "is. Intervallumértékre ez a dátumtartományos átírás nem alkalmazható. A hatást végrehajtási tervvel ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
