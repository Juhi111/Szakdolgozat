using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class ConcatenationInPredicateRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(
            SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.ConcatenationOccurrences)
            {
                string columns = string.Join(", ", occurrence.ColumnReferences);

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "CONCATENATION_IN_PREDICATE",
                    Title = "Keresés összefűzött oszlopértékekben",

                    Description = $"A szűrés || operátorral összekapcsolt kifejezést tartalmaz. Érintett oszlopok: {columns}. Szöveges összefűzés esetén az " +
                        $"egyes oszlopok indexei nem feltétlenül segítik az összefűzött érték keresését. Hely: {occurrence.Location}.",

                    Recommendation = "Ellenőrizd az értékek típusát és azt, hogy valóban az összefűzött eredményre kell-e keresni. Ha külön mezőértékek " +
                        "egyezése a cél, mérlegeld azok külön összehasonlítását. Az elválasztók, a mezőhatárok és a NULL-kezelés miatt ez nem " +
                        "mindig egyenértékű. Gyakori keresésnél a teljes összefűzésre készített kifejezésindex is szóba jöhet; a hatását " +
                        "méréssel ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
