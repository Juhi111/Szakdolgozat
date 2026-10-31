using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class HashBatchRule : IPlanAnalysisRule
    {
        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType != "Hash")
                return findings;

            if (!PlanNodePropertyReader.TryReadDouble(node, "Hash Batches", out double batches))
                return findings;

            if (batches <= 1)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "HASH_MULTIPLE_BATCHES",
                Title = "Több kötegben végzett hash feldolgozás",
                Description = $"A Hash művelet {batches:N0} köteget használt. " +
                    "A több köteg lemezhasználattal is jár, ami növelheti a feldolgozási időt.",
                Recommendation = "Ellenőrizd az érintett összekapcsolás futási idejét és ideiglenes lemezműveleteit. " +
                    "Vizsgáld meg, csökkenthető-e a hash táblába kerülő sorok száma vagy szélessége. " +
                    "Ha a becsült és tényleges sorszám jelentősen eltér, ellenőrizd a statisztikákat is. " +
                    "A memóriabeállításokat csak mérés alapján, a párhuzamos terhelést figyelembe véve módosítsd."
            });

            return findings;
        }
    }
}