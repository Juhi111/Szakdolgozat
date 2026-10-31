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
                Title = "Sok táblahozzáférés index-only beolvasás közben",
                Description = $"Az Index Only Scan művelet {heapFetches:N0} heap sorlekérést végzett. " +
                    "A beolvasás ezért nem tudta teljesen elkerülni a táblához történő hozzáférést. " +
                    "Ez nem ugyanennyi különböző táblalapot vagy fizikai lemezolvasást jelent.",
                Recommendation = "Ellenőrizd a művelet futási idejét és bufferadatait. " +
                    "Vizsgáld meg a tábla módosítási gyakoriságát és a VACUUM/autovacuum működését, " +
                    "amely a visibility map karbantartásában is részt vesz. " +
                    "A gyakran módosított tábláknál a heap-hozzáférés természetes lehet; " +
                    "a magas érték önmagában nem bizonyít hibás indexet vagy rossz végrehajtási tervet."
            });

            return findings;
        }
    }
}