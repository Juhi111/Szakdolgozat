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
                Title = "A kapcsoláshoz készített hash tábla több kötegre oszlott",
                Description = $"A Hash művelet {batches:N0} kötegben dolgozta fel az adatokat. A teljes hash tábla helyett részletekben végzett " +
                    "feldolgozás lemezműveletekkel is jár, ami növelheti a kapcsolás idejét. Az értéket a teljes lekérdezés futási idejével " +
                    "együtt érdemes értékelni.",
                Recommendation = "Ellenőrizd a kapcsolás futási idejét és az ideiglenes fájlok olvasási, írási adatait. Vizsgáld meg, csökkenthető-e a " +
                    "hash táblába kerülő sorok száma vagy a soronként tárolt adatok mennyisége. Jelentős becslési eltérésnél a " +
                    "statisztikákat is ellenőrizd. A memóriakeretet csak a mért igény és az egyidejű terhelés alapján módosítsd."
            });

            return findings;
        }
    }
}
