using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class HashAggregateSpillRule : IPlanAnalysisRule
    {
        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType != "Aggregate")
                return findings;

            if (!PlanNodePropertyReader.TryReadString(node, "Strategy", out string strategy))
                return findings;

            if (strategy != "Hashed" && strategy != "Mixed")
                return findings;

            var details = new List<string>();

            if (PlanNodePropertyReader.TryReadDouble(node, "Disk Usage", out double diskUsage) &&
                diskUsage > 0)
            {
                details.Add($"A csomópont közvetlen adatai szerint a lemezhasználat {diskUsage:N0} kB.");
            }

            foreach (var worker in PlanNodePropertyReader.GetWorkers(node))
            {
                if (!PlanNodePropertyReader.TryReadDouble(worker, "Disk Usage", out double workerDiskUsage) ||
                    workerDiskUsage <= 0)
                {
                    continue;
                }

                string workerLabel = PlanNodePropertyReader.GetWorkerLabel(worker);
                details.Add($"{workerLabel}: {workerDiskUsage:N0} kB lemezhasználat.");
            }

            if (details.Count == 0)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "HASH_AGGREGATE_SPILL",
                Title = "Lemezre kerülő hash aggregálás",
                Description = string.Join(" ", details) +
                    " Ez önmagában nem bizonyít teljesítményproblémát.",
                Recommendation = "Ellenőrizd az aggregálás futási idejét és a feldolgozott adatmennyiséget. " +
                    "Vizsgáld meg, csökkenthető-e az aggregálás bemenete korábbi szűréssel, az eredmény megváltoztatása nélkül. " +
                    "Jelentős sorszámbecslési eltérés esetén ellenőrizd a statisztikákat. " +
                    "A memóriakeret növelésénél vedd figyelembe az egyidejű műveletek és párhuzamos dolgozók memóriaigényét."
            });

            return findings;
        }
    }
}