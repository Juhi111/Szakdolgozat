using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "Összefűzés szűrési feltételben",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + "A WHERE vagy JOIN ON környezetben "
                        + "oszlophivatkozást tartalmazó || lánc szerepel. "
                        + $"Érintett oszlophivatkozások: {columns}. "
                        + "Ha szöveges összefűzésről van szó, a feltétel "
                        + "az összefűzött értéket használja, ezért az "
                        + "egyes oszlopok indexei nem feltétlenül "
                        + "alkalmasak a keresés gyorsítására.",

                    Recommendation =
                        "Ellenőrizd az operandusok típusát és a "
                        + "végrehajtási tervet. Ha az üzleti feltétel "
                        + "külön mezőértékek egyezését keresi, "
                        + "mérlegeld a mezők külön összehasonlítását. "
                        + "Az összefűzött érték nem bontható fel "
                        + "automatikusan: a mezőhatárok, az elválasztók "
                        + "és a NULL-kezelés befolyásolják az eredményt. "
                        + "Ha az összefűzött érték szerinti keresés "
                        + "szükséges és gyakori, vizsgáld meg a teljes "
                        + "kifejezéshez illeszkedő kifejezésindex "
                        + "lehetőségét. A tényleges hatást "
                        + "EXPLAIN (ANALYZE, BUFFERS) segítségével "
                        + "ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}