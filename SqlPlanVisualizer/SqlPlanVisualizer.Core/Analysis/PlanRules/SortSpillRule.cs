using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    /// <summary>
    /// Jelzi, ha egy Sort csomópont lemezhasználatot jelent.
    /// </summary>
    public class SortSpillRule : IPlanAnalysisRule
    {
        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType != "Sort")
                return findings;

            var details = new List<string>();

            if (PlanNodePropertyReader.TryReadString(node, "Sort Space Type", out string spaceType) &&
                spaceType == "Disk")
            {
                string detail = "A Sort művelet saját adatai szerint a rendezés lemezterületet is használt.";

                if (PlanNodePropertyReader.TryReadDouble(node, "Sort Space Used", out double spaceUsed) &&
                    spaceUsed >= 0)
                {
                    detail += $" A tervben jelzett lemezterület: {spaceUsed:N0} kB.";
                }

                details.Add(detail);
            }

            foreach (var worker in PlanNodePropertyReader.GetWorkers(node))
            {
                if (!PlanNodePropertyReader.TryReadString(worker, "Sort Space Type", out string workerSpaceType) ||
                    workerSpaceType != "Disk")
                {
                    continue;
                }

                string detail = $"{PlanNodePropertyReader.GetWorkerLabel(worker)}: a rendezés lemezterületet is használt.";

                if (PlanNodePropertyReader.TryReadDouble(worker, "Sort Space Used", out double workerSpaceUsed) &&
                    workerSpaceUsed >= 0)
                {
                    detail += $" A tervben jelzett lemezterület: {workerSpaceUsed:N0} kB.";
                }

                details.Add(detail);
            }

            if (details.Count == 0)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "SORT_SPILL",
                Title = "A rendezéshez lemezterület is kellett",
                Description = string.Join(" ", details) +
                    " A rendezés nem kizárólag a memóriában történt. A többlet olvasás és írás lassíthatja a műveletet; a futási idő alapján döntsd el, szükséges-e változtatás.",
                Recommendation = "Ellenőrizd a rendezés idejét és az átadott sorok, oszlopok mennyiségét. Ha az eredmény megőrizhető, szűkítsd a rendezés " +
                    "bemenetét; gyakori keresésnél vizsgáld meg, biztosíthatja-e index a kívánt sorrendet. A work_mem memóriakeret célzott " +
                    "emelését csak méréssel próbáld ki, mert több egyidejű művelet és párhuzamos dolgozó együtt jelentős memóriát " +
                    "használhat. A lemezen elfoglalt hely nem azonos az elkerüléséhez szükséges memóriával."
            });

            return findings;
        }
    }
}
