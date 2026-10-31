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
                    "A szekvenciális beolvasás sok sort megvizsgált, majd kiszűrt. " +
                    "Ellenőrizd, segíthet-e a szűréshez illeszkedő index a szükséges sorok közvetlen elérésében. ",

                "Index Scan" or "Index Only Scan" or "Bitmap Heap Scan" =>
                    "Az indexes hozzáférés után további szűrés sok sort eldobott. " +
                    "Hasonlítsd össze az indexelérés feltételét a Filter feltétellel. " +
                    "Gyakori és költséges lekérdezésnél vizsgáld meg egy megfelelő összetett vagy részleges index lehetőségét. ",

                _ =>
                    "Vizsgáld meg, előrébb hozható-e a szűrés az eredmény megváltoztatása nélkül. "
            };

            recommendation +=
                "A döntést a futási idő és a bufferadatok alapján hozd meg; " +
                "a nagy eldobási arány önmagában nem bizonyít rossz végrehajtási tervet.";

            var conditions = new List<string>();

            if (PlanNodePropertyReader.TryReadString(node, "Index Cond", out string indexCondition))
                conditions.Add($"Index Cond: {indexCondition}.");

            if (PlanNodePropertyReader.TryReadString(node, "Recheck Cond", out string recheckCondition))
                conditions.Add($"Recheck Cond: {recheckCondition}.");

            if (PlanNodePropertyReader.TryReadString(node, "Filter", out string filter))
                conditions.Add($"Filter: {filter}.");

            string conditionDetails = conditions.Count > 0
                ? " " + string.Join(" ", conditions)
                : string.Empty;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "HIGH_FILTER_REMOVAL",
                Title = "Sok sort eldobó szűrés",
                Description = $"A(z) {node.NodeType} művelet a jelentett átlagok alapján " +
                $"összesen körülbelül {totalRemovedRows:N0} sort dobott el. " +
                $"Az eldobott sorok aránya a kimeneti és kiszűrt sorok együttes számához képest {removalRatio:P1}." + conditionDetails,
                Recommendation = recommendation
            });
            return findings;
        }
    }
}