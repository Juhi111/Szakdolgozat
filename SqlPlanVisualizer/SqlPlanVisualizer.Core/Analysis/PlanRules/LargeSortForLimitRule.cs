using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.PlanRules
{
    public class LargeSortForLimitRule : IPlanAnalysisRule
    {
        private readonly double _minimumInputRows;
        private readonly double _inputOutputRatioThreshold;

        public LargeSortForLimitRule(double minimumInputRows, double inputOutputRatioThreshold)
        {
            if (!double.IsFinite(minimumInputRows) || minimumInputRows <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumInputRows));

            if (!double.IsFinite(inputOutputRatioThreshold) || inputOutputRatioThreshold <= 1)
                throw new ArgumentOutOfRangeException(nameof(inputOutputRatioThreshold));

            _minimumInputRows = minimumInputRows;
            _inputOutputRatioThreshold = inputOutputRatioThreshold;
        }

        public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var findings = new List<PlanAnalysisFinding>();

            if (node.NodeType != "Limit")
                return findings;

            var sort = FindOuterChild(node);

            if (sort?.NodeType == "Gather Merge")
                sort = FindOuterChild(sort);

            if (sort?.NodeType != "Sort")
                return findings;

            var input = FindOuterChild(sort);

            if (input is null ||
                !TryReadTotalRows(input, out double inputRows) ||
                !TryReadTotalRows(node, out double outputRows))
            {
                return findings;
            }

            if (outputRows <= 0 || inputRows < _minimumInputRows)
                return findings;

            double ratio = inputRows / outputRows;

            if (!double.IsFinite(ratio) || ratio < _inputOutputRatioThreshold)
                return findings;

            string recommendation = input.NodeType == "Aggregate"
                ? "A rendezés már összesített adatokat kapott. Nézd meg, csökkenthető-e az összesítéshez feldolgozott sorok száma " +
                  "az eredmény megváltoztatása nélkül. Az aggregált érték szerinti sorrendet egy alapadatokon " +
                  "létrehozott index nem feltétlenül szolgálja ki. "
                : "Nézd meg, lehet-e kevesebb adatot átadni a rendezésnek az eredmény megőrzésével. Gyakori lekérdezésnél ellenőrizd, " +
                  "kiszolgálható-e a szűrés és a kívánt sorrend megfelelő indexszel. ";

            recommendation +=
                "A rendezés tényleges ideje alapján értékeld, mekkora gondot jelent ez az adatmennyiség. " +
                "Vedd figyelembe a futási időt és az esetleges OFFSET értékét is.";

            findings.Add(new PlanAnalysisFinding(node)
            {
                RuleId = "LARGE_SORT_FOR_LIMIT",
                Title = "A sorlimit ellenére sok adat került a rendezésbe",
                Description = $"A rendezés körülbelül {inputRows:N0} bemeneti sort kapott, miközben a LIMIT összesen {outputRows:N0} sort adott tovább. " +
                    $"Ez {ratio:N0}-szeres bemenet/kimenet arány. Az értékeket a jelentett átlagokból és végrehajtásszámokból számítottuk. A " +
                    "kevés visszaadott sor tehát nem jelentett kevés feldolgozandó adatot.",
                Recommendation = recommendation
            });

            return findings;
        }

        private static PlanNode? FindOuterChild(PlanNode node)
        {
            PlanNode? result = null;

            foreach (var child in node.Children)
            {
                if (!PlanNodePropertyReader.TryReadString(child, "Parent Relationship", out string relationship) ||
                    relationship != "Outer")
                {
                    continue;
                }

                if (result != null)
                    return null;

                result = child;
            }

            return result;
        }

        private static bool TryReadTotalRows(PlanNode node, out double totalRows)
        {
            totalRows = 0;

            if (node.ActualRows is not double rows || node.ActualLoops is not double loops)
                return false;

            if (!double.IsFinite(rows) || rows < 0 || !double.IsFinite(loops) || loops <= 0)
                return false;

            totalRows = rows * loops;
            return double.IsFinite(totalRows);
        }
    }
}
