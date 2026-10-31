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
                string detail = $"A csomópont által jelentett veszteséges blokkok száma: {lossyBlocks:N0}.";

                if (PlanNodePropertyReader.TryReadDouble(node, "Exact Heap Blocks", out double exactBlocks) &&
                    exactBlocks >= 0)
                {
                    detail += $" Pontos blokkok száma: {exactBlocks:N0}.";
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
                string detail = $"{label}: {workerLossyBlocks:N0} veszteséges blokk.";

                if (PlanNodePropertyReader.TryReadDouble(worker, "Exact Heap Blocks", out double workerExactBlocks) &&
                    workerExactBlocks >= 0)
                {
                    detail += $" Pontos blokkok száma: {workerExactBlocks:N0}.";
                }

                details.Add(detail);
            }

            if (details.Count == 0)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "LOSSY_BITMAP_HEAP_SCAN",
                Title = "Veszteséges bitmap beolvasás",
                Description = string.Join(" ", details) +
                    " A veszteséges blokkoknál a bitmap csak a lapot azonosítja, " +
                    "ezért a sorokon újra ellenőrizni kell az indexfeltételt. Ez nem jelent adatvesztést.",
                Recommendation = "Ellenőrizd a művelet futási idejét és a Rows Removed by Index Recheck értékét. " +
                    "Vizsgáld meg az alkalmazott index típusát és a feltétel szelektivitását. " +
                    "Ha a veszteséges bitmap a szűk memóriakeret miatt alakult ki, " +
                    "mérlegeld a work_mem célzott emelését a párhuzamos terhelés figyelembevételével. " +
                    "A veszteséges blokkok jelenléte önmagában nem bizonyít rossz végrehajtási tervet."
            });
            return findings;
        }
    }
}