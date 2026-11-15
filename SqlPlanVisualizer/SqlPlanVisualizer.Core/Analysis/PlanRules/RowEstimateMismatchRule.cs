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

            if (!double.IsFinite(estimated) || !double.IsFinite(actual) || !double.IsFinite(loops) || estimated <= 0 || actual < 0 || loops <= 0)
                return findings;

            double? ratio = actual > 0
                ? Math.Max(estimated, actual) / Math.Min(estimated, actual)
                : null;
            double difference = Math.Abs(actual - estimated);

            if ((ratio.HasValue && ratio.Value < _ratioThreshold) || difference < _differenceThreshold)
                return findings;

            string direction = actual > estimated ? "több" : "kevesebb";

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "ROW_ESTIMATE_MISMATCH",
                Title = "A visszaadott sorok száma jelentősen eltért a becsléstől",
                Description =
                    $"A(z) {node.NodeType} művelethez a tervező {estimated:N0} kimeneti sort becsült, " +
                    $"a végrehajtás viszont alkalmanként átlagosan {actual:N0} sort adott tovább. " +
                    (ratio.HasValue
                        ? $"A mért és a becsült érték eltérési aránya {ratio.Value:N1}-szeres; a mért sorszám {direction} a becsültnél."
                        : "A jelentett tényleges sorszám nulla; ezért eltérési arányt nem számítunk. Ez a jelentett átlag, nem feltétlenül minden végrehajtás pontos sorszáma."),
                Recommendation = "Először ellenőrizd, teljesen lefutott-e a művelet: a LIMIT vagy egy létezésvizsgálat korán leállíthatja a sorok " +
                    "lekérését. Ha valóban pontatlan volt a becslés, nézd meg a táblák statisztikáinak frissességét és a szűrés feltételeit. " +
                    "Az ANALYZE frissítheti a statisztikákat; összefüggő oszlopoknál kiterjesztett statisztika is segíthet. Az eltérés " +
                    "jelentőségét a választott terv és a futási idő alapján értékeld."
            });

            if (actual < estimated && HasPotentialLimitingAncestor(context))
            {
                findings[0].Description +=
                    " A tervben fölötte LIMIT található, amely már elegendő eredménysor után leállíthatta a sorok lekérését. " +
                    "A kisebb tényleges sorszám ezért önmagában nem bizonyít pontatlan becslést.";

                findings[0].Recommendation =
                    "A fölérendelt LIMIT miatt kezdd a korai leállás ellenőrzésével. " +
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
