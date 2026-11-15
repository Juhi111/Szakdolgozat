using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class LossyBitmapHeapScanRule : IPlanAnalysisRule
    {
        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType is not "Bitmap Heap Scan")
                return findings;

            var details = new List<string>();

            if (PlanNodePropertyReader.TryReadDouble(node, "Lossy Heap Blocks", out double lossyBlocks) &&
                lossyBlocks > 0)
            {
                string detail = $"Csak lap szinten azonosított blokkok (Lossy Heap Blocks): {lossyBlocks:N0}.";

                if (PlanNodePropertyReader.TryReadDouble(node, "Exact Heap Blocks", out double exactBlocks) &&
                    exactBlocks >= 0)
                {
                    detail += $" Pontos sorhelyeket tartalmazó blokkok (Exact Heap Blocks): {exactBlocks:N0}.";
                }

                details.Add(detail);
            }

            foreach (var worker in PlanNodePropertyReader.GetWorkers(node))
            {
                if (!PlanNodePropertyReader.TryReadDouble(worker, "Lossy Heap Blocks", out double workerLossyBlocks) ||
                    workerLossyBlocks <= 0)
                {
                    continue;
                }

                string label = PlanNodePropertyReader.GetWorkerLabel(worker);
                string detail = $"{label}: {workerLossyBlocks:N0} lap szinten azonosított (lossy) blokk.";

                if (PlanNodePropertyReader.TryReadDouble(worker, "Exact Heap Blocks", out double workerExactBlocks) &&
                    workerExactBlocks >= 0)
                {
                    detail += $" Pontos sorhelyeket tartalmazó blokkok (Exact Heap Blocks): {workerExactBlocks:N0}.";
                }

                details.Add(detail);
            }

            if (details.Count == 0)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "LOSSY_BITMAP_HEAP_SCAN",
                Title = "A bitmap beolvasásnál teljes táblalapokat kellett ellenőrizni",
                Description = string.Join(" ", details) +
                    " A lossy jelölésnél a bitmap csak a megfelelő táblalapot ismeri, nem a pontos sorhelyeket, " +
                    "ezért a sorokon újra ellenőrizni kell az indexfeltételt. Ez nem jelent adatvesztést.",
                Recommendation = "Nézd meg a futási időt és a Rows Removed by Index Recheck mezőt: ez mutatja az indexfeltétel újraellenőrzésekor " +
                    "eldobott sorokat. Ellenőrizd az index típusát és azt, mennyire szűkíti a keresést a feltétel. Ha a lap szintű jelölés " +
                    "oka a kevés memória, próbáld ki a work_mem célzott emelését, az egyidejű terhelés figyelembevételével. A lossy blokkok " +
                    "jelenléte önmagában nem hibás terv."
            });
            return findings;
        }
    }
}
