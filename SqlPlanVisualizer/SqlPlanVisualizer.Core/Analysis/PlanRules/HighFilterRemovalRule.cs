using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class HighFilterRemovalRule : IPlanAnalysisRule
    {
        private readonly double _minimumRemovedRows;
        private readonly double _removalRatioThreshold;

        public HighFilterRemovalRule(double minimumRemovedRows, double removalRatioThreshold)
        {
            if (!double.IsFinite(minimumRemovedRows) || minimumRemovedRows <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumRemovedRows));

            if (!double.IsFinite(removalRatioThreshold) ||
                removalRatioThreshold <= 0 ||
                removalRatioThreshold > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(removalRatioThreshold));
            }

            _minimumRemovedRows = minimumRemovedRows;
            _removalRatioThreshold = removalRatioThreshold;
        }

        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (!PlanNodePropertyReader.TryReadDouble(node, "Rows Removed by Filter", out double removedRows))
                return findings;

            if (node.ActualRows is not double actualRows || node.ActualLoops is not double loops)
                return findings;

            if (removedRows <= 0 || !double.IsFinite(actualRows) || actualRows < 0 ||
                !double.IsFinite(loops) || loops <= 0)
            {
                return findings;
            }

            double totalRemovedRows = removedRows * loops;
            double consideredRows = removedRows + actualRows;

            if (!double.IsFinite(totalRemovedRows) || !double.IsFinite(consideredRows))
                return findings;

            double removalRatio = removedRows / consideredRows;

            if (totalRemovedRows < _minimumRemovedRows || removalRatio < _removalRatioThreshold)
                return findings;

            string recommendation = node.NodeType switch
            {
                "Seq Scan" =>
                    "A Seq Scan sorban olvassa a táblát, majd ellenőrzi a feltételt. " +
                    "Ellenőrizd, segíthet-e a szűréshez illeszkedő index a szükséges sorok közvetlen elérésében. ",

                "Index Scan" or "Index Only Scan" or "Bitmap Heap Scan" =>
                    "Az index használata után is sok sor fennakadt a további szűrésen. " +
                    "Hasonlítsd össze az Index Cond (indexkeresés) és a Filter (utólagos szűrés) feltételeit. " +
                    "Gyakori és költséges lekérdezésnél vizsgáld meg egy megfelelő összetett vagy részleges index lehetőségét. ",

                _ =>
                    "Ellenőrizd, csökkenthető-e korábbi szűréssel a művelet bemenete, az eredmény megőrzésével. "
            };

            recommendation +=
                "A futási idő és a Buffers mezők olvasási adatai alapján döntsd el, érdemes-e változtatni; " +
                "a nagy eldobási arány önmagában nem bizonyít rossz végrehajtási tervet.";

            var conditions = new List<string>();

            if (PlanNodePropertyReader.TryReadString(node, "Index Cond", out string indexCondition))
                conditions.Add($"Indexkeresési feltétel (Index Cond): {indexCondition}.");

            if (PlanNodePropertyReader.TryReadString(node, "Recheck Cond", out string recheckCondition))
                conditions.Add($"Újraellenőrzött feltétel (Recheck Cond): {recheckCondition}.");

            if (PlanNodePropertyReader.TryReadString(node, "Filter", out string filter))
                conditions.Add($"Szűrési feltétel (Filter): {filter}.");

            string conditionDetails = conditions.Count > 0
                ? " " + string.Join(" ", conditions)
                : string.Empty;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "HIGH_FILTER_REMOVAL",
                Title = "A szűrés a feldolgozott sorok nagy részét eldobta",
                Description = $"A(z) {node.NodeType} művelet a végrehajtásonkénti átlagok és az ismétlések száma alapján összesen körülbelül " +
                    $"{totalRemovedRows:N0} sort szűrt ki. A kiszűrt és továbbadott sorok együttes számából {removalRatio:P1} volt a kiszűrt " +
                    "rész." + conditionDetails,
                Recommendation = recommendation
            });
            return findings;
        }
    }
}
