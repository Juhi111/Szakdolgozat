using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class HighHeapFetchesRule : IPlanAnalysisRule
    {
        private readonly double _minimumHeapFetches;

        public HighHeapFetchesRule(double minimumHeapFetches)
        {
            if (!double.IsFinite(minimumHeapFetches) || minimumHeapFetches <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumHeapFetches));

            _minimumHeapFetches = minimumHeapFetches;
        }

        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType != "Index Only Scan")
                return findings;

            if (!PlanNodePropertyReader.TryReadDouble(node, "Heap Fetches", out double heapFetches))
                return findings;

            if (heapFetches < _minimumHeapFetches)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "HIGH_HEAP_FETCHES",
                Title = "Az index mellett a táblát is sokszor el kellett érni",
                Description = $"Az Index Only Scan {heapFetches:N0} alkalommal kért le sort a táblából (Heap Fetches), így a művelet nem tudott " +
                    "kizárólag az index adataira támaszkodni. Ez a szám nem különböző táblalapokat vagy fizikai lemezolvasásokat jelent.",
                Recommendation = "Ellenőrizd a futási időt és a Buffers mezők olvasási adatait. A visibility map jelzi, mely táblalapokon hagyható el a " +
                    "sorok láthatóságának külön ellenőrzése; ezt a VACUUM is karbantartja. Nézd meg az autovacuum működését és a tábla " +
                    "módosítási gyakoriságát. Gyakran változó táblán a táblahozzáférés természetes lehet, és a magas érték nem bizonyít " +
                    "hibás indexet."
            });

            return findings;
        }
    }
}
