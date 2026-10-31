using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class RepeatedSequentialScanRule : IPlanAnalysisRule
    {
        private readonly double _minimumLoops;
        private readonly double _minimumExaminedRows;

        public RepeatedSequentialScanRule(double minimumLoops, double minimumExaminedRows)
        {
            if (!double.IsFinite(minimumLoops) || minimumLoops <= 1)
                throw new ArgumentOutOfRangeException(nameof(minimumLoops));

            if (!double.IsFinite(minimumExaminedRows) || minimumExaminedRows <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumExaminedRows));

            _minimumLoops = minimumLoops;
            _minimumExaminedRows = minimumExaminedRows;
        }

        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType != "Seq Scan" || context is null)
                return findings;

            if (node.ActualLoops is not double loops ||
                !double.IsFinite(loops) || loops < _minimumLoops)
            {
                return findings;
            }

            if (node.ActualRows is not double actualRows ||
                !double.IsFinite(actualRows) || actualRows < 0)
            {
                return findings;
            }

            if (!TryReadExecutionContext(context, out string executionContext))
                return findings;

            double removedRows = 0;

            if (node.AdditionalProperties.ContainsKey("Rows Removed by Filter"))
            {
                if (!PlanNodePropertyReader.TryReadDouble(node, "Rows Removed by Filter", out removedRows) ||
                    removedRows < 0)
                {
                    return findings;
                }
            }

            double examinedRows = (actualRows + removedRows) * loops;

            if (!double.IsFinite(examinedRows) || examinedRows < _minimumExaminedRows)
                return findings;

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "REPEATED_SEQUENTIAL_SCAN",
                Title = "Sokszor ismételt szekvenciális beolvasás",
                Description = $"A Seq Scan művelet {loops:N0} alkalommal futott " +
                    $"({executionContext}). A jelentett sorszámok alapján összesen körülbelül " +
                    $"{examinedRows:N0} sort vizsgált meg. Ugyanazok a sorok többször is beleszámíthatnak.",
                Recommendation = "Ellenőrizd a beolvasás szűrőfeltételét és az ismétlések futásidejét. " +
                    "Vizsgáld meg, segíthet-e a belső keresési kulcshoz illeszkedő index. " +
                    "Korrelált al-lekérdezésnél mérlegeld az egyszeri előaggregálás vagy összekapcsolás lehetőségét, " +
                    "a NULL-értékek és duplikációk kezelésének megőrzésével. " +
                    "A Nested Loop vagy a korrelált lekérdezés önmagában nem hibás."
            });

            return findings;
        }

        private static bool TryReadExecutionContext(PlanAnalysisContext context, out string description)
        {
            description = string.Empty;
            var current = context;

            while (current.Parent != null)
            {
                if (PlanNodePropertyReader.TryReadString(current.Node, "Parent Relationship", out string relationship))
                {
                    if (relationship == "InitPlan")
                        return false;

                    if (relationship == "SubPlan")
                    {
                        description = "SubPlan ágában";
                        return true;
                    }

                    if (relationship == "Inner" && current.Parent.Node.NodeType == "Nested Loop")
                    {
                        description = "Nested Loop belső ágában";
                        return true;
                    }
                }

                current = current.Parent;
            }

            return false;
        }
    }
}