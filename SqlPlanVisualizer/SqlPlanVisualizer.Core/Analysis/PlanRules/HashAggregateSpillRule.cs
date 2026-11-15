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
                details.Add($"Az Aggregate művelet saját adatai szerint a lemezhasználat {diskUsage:N0} kB.");
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
                Title = "Az összesítéshez lemezterület is kellett",
                Description = string.Join(" ", details) +
                    " A hash alapú összesítés adatainak egy része lemezre került. Ez többlet I/O-munkát jelenthet; a futási idő mutatja meg, hogy érdemes-e beavatkozni.",
                Recommendation = "Nézd meg, mennyi időt töltött a művelet az összesítéssel, és mennyi adatot kapott. Korábbi szűréssel esetenként " +
                    "csökkenthető a bemenet, ha az eredmény változatlan marad. Nagy becslési eltérésnél ellenőrizd a táblák statisztikáit. A " +
                    "memóriakeretet csak mérés alapján módosítsd: az egyidejű műveletek és párhuzamos dolgozók együtt több memóriát " +
                    "használhatnak."
            });

            return findings;
        }
    }
}
