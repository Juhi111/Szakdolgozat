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
                Title = "A sorlimit mellett is nagy a rendezési bemenet",
                SqlObservation =
                    "Az SQL felső szintű lekérdezése rendezést és sorlimitet tartalmaz.",
                PlanObservation = planFinding.Description,
                Description =
                    "A párosított terv felső szintű Limit műveletéhez nagy rendezési bemenet " +
                    "tartozik a visszaadott sorok számához képest. Ebben a végrehajtásban " +
                    "a kis eredményhalmaz előállításához sok bemeneti sort kellett feldolgozni.",
                Recommendation = planFinding.Recommendation,
                RelatedPlanFinding = planFinding
            });

            return findings;
        }
    }
}