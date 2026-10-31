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
                Title = "A tervezettnél kevesebb párhuzamos dolgozó",
                Description = $"A(z) {node.NodeType} művelethez {planned:N0} dolgozót tervezett a PostgreSQL, " +
                    $"de csak {launched:N0} indult el. " +
                    "Ez csökkentheti a tervben feltételezett párhuzamosságot.",
                Recommendation = "Ellenőrizd a lekérdezés futási idejét és a szerver egyidejű terhelését. " +
                    "Vizsgáld meg a párhuzamos dolgozók rendelkezésre állását, valamint a max_worker_processes " +
                    "és max_parallel_workers beállításokat. " +
                    "A limitek emelését csak a rendelkezésre álló CPU- és memóriakapacitás alapján mérlegeld. " +
                    "A dolgozók számának eltérése önmagában nem bizonyít teljesítményproblémát."
            });
            return findings;
        }
    }
}