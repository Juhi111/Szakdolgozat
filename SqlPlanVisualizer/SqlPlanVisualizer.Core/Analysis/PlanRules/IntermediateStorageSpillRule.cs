using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class IntermediateStorageSpillRule : IPlanAnalysisRule
    {
        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (!PlanNodePropertyReader.TryReadString(node, "Storage", out string storage) || storage != "Disk")
                return findings;

            string storageDetails = string.Empty;

            if (PlanNodePropertyReader.TryReadDouble(node, "Maximum Storage", out double maximumStorage) && maximumStorage >= 0)
            {
                storageDetails = $" A jelentett maximális tárhelyhasználat {maximumStorage:N0} kB.";
            }

            string recommendation = node.NodeType switch
            {
                "Materialize" =>
                    "Vizsgáld meg, csökkenthető-e a tárolt sorok száma vagy szélessége. " +
                    "Ellenőrizd az ismételt felhasználás szerepét: a köztes eredmény tárolása hasznos is lehet.",

                "CTE Scan" =>
                    "Vizsgáld meg a CTE eredményének méretét és felhasználási helyeit. " +
                    "Ellenőrizd, hogy a szűrés korábbra helyezhető-e az eredmény megváltoztatása nélkül. " +
                    "A CTE Scan jelenléte önmagában nem indokolja a materializálás megszüntetését.",

                "WindowAgg" =>
                    "Vizsgáld meg az ablakfüggvény bemenetének méretét, a partíciókat és az ablakkeretet. " +
                    "A bemeneti sorok vagy oszlopok csökkentését csak az eredmény megőrzésével alkalmazd.",

                _ =>
                    "Vizsgáld meg, csökkenthető-e a köztes eredmény mérete az eredmény megváltoztatása nélkül."
            };

            recommendation +=
                " Ellenőrizd a futási időt és az ideiglenes lemezműveleteket. " +
                "A work_mem módosítását méréssel és a párhuzamos memóriaigény figyelembevételével mérlegeld. " +
                "A jelentett lemezterület nem azonos a lemezhasználat elkerüléséhez szükséges memóriával.";

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "INTERMEDIATE_STORAGE_SPILL",
                Title = "Köztes eredmény lemezes tárolása",
                Description =
                    $"A(z) {node.NodeType} művelet lemezes tárolást jelentett a köztes eredményhez." +
                    storageDetails +
                    " Ez önmagában nem bizonyít teljesítményproblémát.",
                Recommendation = recommendation
            });

            return findings;
        }
    }
}