using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class ParallelWorkerShortageRule : IPlanAnalysisRule
    {
        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType != "Gather" && node.NodeType != "Gather Merge")
                return findings;

            if (node.ActualLoops is not double loops || !double.IsFinite(loops) || loops <= 0)
                return findings;

            if (!PlanNodePropertyReader.TryReadDouble(node, "Workers Planned", out double planned))
                return findings;

            if (!PlanNodePropertyReader.TryReadDouble(node, "Workers Launched", out double launched))
                return findings;

            if (planned <= 0 || launched < 0 || launched >= planned)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "PARALLEL_WORKER_SHORTAGE",
                Title = "A tervezettnél kevesebb párhuzamos dolgozó indult",
                Description = $"A(z) {node.NodeType} művelethez tervezett {planned:N0} párhuzamos dolgozóból {launched:N0} indult el. Így kevesebb " +
                    "dolgozó osztozott a munkán, mint amennyivel a tervező számolt. A futási időre gyakorolt hatás a terheléstől is függ.",
                Recommendation = "Ellenőrizd a futási időt és azt, milyen más lekérdezések futottak ugyanakkor. Nézd meg a rendelkezésre álló párhuzamos " +
                    "dolgozókat, valamint a max_worker_processes és max_parallel_workers korlátokat. Csak akkor emelj a limiteken, ha a CPU- " +
                    "és memóriakapacitás is elegendő. Egyetlen ilyen futás még nem bizonyít tartós teljesítményproblémát."
            });
            return findings;
        }
    }
}
