using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    /// <summary>
    /// Jelzi a becsült és tényleges kimeneti sorszám jelentős eltérését.
    /// </summary>
    public class RowEstimateMismatchRule : IPlanAnalysisRule
    {
        private readonly double _ratioThreshold;
        private readonly double _differenceThreshold;

        /// <summary>
        /// Létrehozza a szabályt a megadott eltérési küszöbökkel.
        /// </summary>
        /// <param name="ratioThreshold">A jelzéshez szükséges minimális arány, egynél nagyobb érték.</param>
        /// <param name="differenceThreshold">A jelzéshez szükséges minimális pozitív sorkülönbség.</param>
        /// <exception cref="ArgumentOutOfRangeException">Valamelyik küszöb érvénytelen.</exception>
        public RowEstimateMismatchRule(double ratioThreshold, double differenceThreshold)
        {
            if (!double.IsFinite(ratioThreshold) || ratioThreshold <= 1)
                throw new ArgumentOutOfRangeException(nameof(ratioThreshold));

            if (!double.IsFinite(differenceThreshold) || differenceThreshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(differenceThreshold));

            _ratioThreshold = ratioThreshold;
            _differenceThreshold = differenceThreshold;
        }

        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            var findings = new List<PlanAnalysisFinding>();

            if (node.PlanRows is not double estimated || node.ActualRows is not double actual || node.ActualLoops is not double loops)
                return findings;

            if (!double.IsFinite(estimated) || !double.IsFinite(actual) || !double.IsFinite(loops) || estimated <= 0 || actual <= 0 || loops <= 0)
                return findings;

            double ratio = Math.Max(estimated, actual) / Math.Min(estimated, actual);
            double difference = Math.Abs(actual - estimated);

            if (ratio < _ratioThreshold || difference < _differenceThreshold)
                return findings;

            string direction = actual > estimated ? "több" : "kevesebb";

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "ROW_ESTIMATE_MISMATCH",
                Title = "Jelentős eltérés a becsült és tényleges sorszám között",
                Description =
                    $"A(z) {node.NodeType} művelet becsült kimenete {estimated:N0} sor, " +
                    $"a tényleges kimenete végrehajtásonként átlagosan {actual:N0} sor. " +
                    $"A tényleges érték {ratio:N1}-szeres arányban {direction} a becsültnél.",
                Recommendation =
                    "Ellenőrizd, hogy a művelet teljesen lefutott-e: például egy LIMIT " +
                    "vagy létezésvizsgálat miatt a végrehajtás korábban megállhatott. " +
                    "Ha az eltérés becslési pontatlanságból ered, ellenőrizd az érintett " +
                    "táblák statisztikáinak frissességét és a szűrési feltételeket. " +
                    "Szükség esetén vizsgáld meg az ANALYZE és az összefüggő oszlopokhoz " +
                    "tartozó kiterjesztett statisztikák alkalmazhatóságát. " +
                    "Az eltérés önmagában nem bizonyít teljesítményproblémát."
            });

            if (actual < estimated && HasPotentialLimitingAncestor(context))
            {
                findings[0].Description +=
                    " A művelet fölött LIMIT található, amely korábban leállíthatta a sorok lekérését. " +
                    "A kisebb tényleges sorszám ezért önmagában nem bizonyít pontatlan becslést.";

                findings[0].Recommendation =
                    "A statisztikák módosítása előtt ellenőrizd, hogy a művelet teljes bemenetét feldolgozta-e. " +
                    findings[0].Recommendation;
            }




            return findings;
        }

        /// <summary>
        /// Megvizsgálja, hogy egy fölérendelt LIMIT korlátozhatta-e
        /// az aktuális művelettől elkért sorok számát.
        /// </summary>
        private static bool HasPotentialLimitingAncestor(PlanAnalysisContext? context)
        {
            var current = context;

            while (current?.Parent != null)
            {
                if (!PlanNodePropertyReader.TryReadString(current.Node, "Parent Relationship", out string relationship) ||
                    relationship != "Outer")
                {
                    return false;
                }

                var parent = current.Parent;
                var parentNode = parent.Node;

                if (parentNode.NodeType == "Limit")
                {
                    return parentNode.ActualLoops is double loops &&
                        double.IsFinite(loops) &&
                        loops > 0;
                }

                if (parentNode.NodeType != "Result" && parentNode.NodeType != "Subquery Scan")
                    return false;

                current = parent;
            }

            return false;
        }


    }
}