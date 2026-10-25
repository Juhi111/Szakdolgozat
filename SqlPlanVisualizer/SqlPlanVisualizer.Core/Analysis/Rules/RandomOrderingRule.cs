using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class RandomOrderingRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.RandomOrderings)
            {
                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "RANDOM_ORDERING",
                    Title = "Véletlen érték szerinti rendezés",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        "A SELECT ORDER BY listájában közvetlen random() függvényhívás szerepel. " +
                        "A véletlen rendezési értékek előállítása és a rendezés nagy bemenetnél költséges lehet. " +
                        "A LIMIT önmagában nem garantálja, hogy csak a visszaadott sorokat kell feldolgozni.",
                    Recommendation =
                        "Ellenőrizd a feldolgozott sorszámokat és a rendezési műveletet " +
                        "EXPLAIN (ANALYZE, BUFFERS) segítségével. " +
                        "Ha csak véletlen mintára van szükség, mérlegeld a TABLESAMPLE vagy más " +
                        "mintavételi stratégia alkalmazhatóságát. Ezek nem automatikusan egyenértékűek: " +
                        "eltérhet a mintanagyság, az eloszlás és a szűréssel való kapcsolatuk. " +
                        "Ha a teljes eredmény véletlen sorrendje szükséges, vagy a bemenet kicsi, " +
                        "a jelenlegi megoldás indokolt lehet.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}