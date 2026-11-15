using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules
{
    /// <summary>
    /// A felső szintű rendezést és sorlimitet a nagy rendezési bemenettel kapcsolja össze.
    /// </summary>
    public class OrderedLimitWithLargeSortRule : ICombinedAnalysisRule
    {
        public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
        {
            ArgumentNullException.ThrowIfNull(analysis);

            var findings = new List<CombinedAnalysisFinding>();
            var query = analysis.Pair.Statement.TopLevelQuery;
            var root = analysis.Pair.Plan.Root;

            if (query is null || query.IsSetOperation ||
                !query.HasOrderBy || !query.HasRowLimit || root.NodeType != "Limit")
            {
                return findings;
            }

            var planFinding = analysis.PlanFindings.FirstOrDefault(finding =>
                finding.RuleId == "LARGE_SORT_FOR_LIMIT" &&
                ReferenceEquals(finding.Node, root));

            if (planFinding is null)
                return findings;

            findings.Add(new CombinedAnalysisFinding
            {
                RuleId = "ORDERED_LIMIT_WITH_LARGE_SORT",
                Title = "Kevés kért eredménysor mellett is sok adatot kapott a rendezés",
                SqlObservation = "Az SQL fő lekérdezése meghatározott sorrendben, korlátozott számú sort kér vissza.",
                PlanObservation = planFinding.Description,
                Description = "A párosított terv Limit művelete alatt a rendezés jóval több bemeneti sort kapott, mint amennyi végül az eredménybe " +
                    "került. A sorlimit tehát ebben a futásban nem csökkentette ugyanilyen mértékben a feldolgozandó adatokat. A futási idő " +
                    "és a hozzáférés módja alapján érdemes megvizsgálni az optimalizálást.",
                Recommendation = planFinding.Recommendation,
                RelatedPlanFinding = planFinding
            });

            return findings;
        }
    }
}
