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
                string detail = "A csomópont közvetlen adatai lemezes rendezést jeleznek.";

                if (PlanNodePropertyReader.TryReadDouble(node, "Sort Space Used", out double spaceUsed) &&
                    spaceUsed >= 0)
                {
                    detail += $" Jelentett lemezterület: {spaceUsed:N0} kB.";
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

                string detail = $"{PlanNodePropertyReader.GetWorkerLabel(worker)}: lemezes rendezés.";

                if (PlanNodePropertyReader.TryReadDouble(worker, "Sort Space Used", out double workerSpaceUsed) &&
                    workerSpaceUsed >= 0)
                {
                    detail += $" Jelentett lemezterület: {workerSpaceUsed:N0} kB.";
                }

                details.Add(detail);
            }

            if (details.Count == 0)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "SORT_SPILL",
                Title = "Lemezre kerülő rendezés",
                Description = string.Join(" ", details) +
                    " Ez önmagában nem bizonyít teljesítményproblémát.",
                Recommendation = "Ellenőrizd a rendezés idejét és a rendezendő adatmennyiséget. " +
                    "Vizsgáld meg, csökkenthető-e a sorok száma vagy szélessége, illetve kiszolgálható-e a kívánt sorrend indexszel. " +
                    "A work_mem célzott emelésénél vedd figyelembe az egyidejű műveletek és párhuzamos dolgozók memóriaigényét. " +
                    "A jelentett lemezterület nem azonos a lemezhasználat elkerüléséhez szükséges memóriával."
            });

            return findings;
        }
    }
}