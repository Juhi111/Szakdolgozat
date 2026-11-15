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
                storageDetails = $" A tervben jelzett legnagyobb tárhelyhasználat {maximumStorage:N0} kB.";
            }

            string recommendation = node.NodeType switch
            {
                "Materialize" =>
                    "Nézd meg, kevesebb sor vagy kevesebb oszlop tárolásával is előállítható-e ugyanaz az eredmény. " +
                    "Ellenőrizd az ismételt felhasználás szerepét: a köztes eredmény tárolása hasznos is lehet.",

                "CTE Scan" =>
                    "Nézd meg, mennyi adatot ad a WITH részben megadott lekérdezés (CTE), és hol használja azt a teljes lekérdezés. " +
                    "Ellenőrizd, hogy a szűrés korábbra helyezhető-e az eredmény megváltoztatása nélkül. " +
                    "A CTE Scan jelenléte önmagában nem indokolja a materializálás megszüntetését.",

                "WindowAgg" =>
                    "Ellenőrizd, hány soron dolgozik az ablakfüggvény, és mekkora csoportokat, illetve ablakkeretet használ. " +
                    "A bemeneti sorok vagy oszlopok csökkentését csak az eredmény megőrzésével alkalmazd.",

                _ =>
                    "Ellenőrizd, tárolható-e kevesebb köztes adat úgy, hogy a végső eredmény azonos maradjon."
            };

            recommendation +=
                " A futási idő és az ideiglenes fájlok olvasási, írási adatai mutatják meg ennek jelentőségét. " +
                "A work_mem a műveletek memóriakeretét befolyásolja; emelését csak mérés alapján, az egyidejű terhelést is figyelembe véve próbáld ki. " +
                "A jelentett lemezterület nem azonos a lemezhasználat elkerüléséhez szükséges memóriával.";

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "INTERMEDIATE_STORAGE_SPILL",
                Title = "A köztes eredményt lemezen tárolta a művelet",
                Description = $"A(z) {node.NodeType} művelet a további feldolgozáshoz szükséges köztes adatokat lemezen tárolta." +
                    storageDetails +
                    " Ez többlet olvasással és írással járhat, de a tárolás az ismételt számítás elkerülését is segítheti.",
                Recommendation = recommendation
            });

            return findings;
        }
    }
}
