using System;
using System.Collections.Generic;
using System.Linq;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules
{
    /// <summary>
    /// A felső szintű rendezést összekapcsolja annak lemezhasználatával.
    /// </summary>
    public class OrderByWithSortSpillRule : ICombinedAnalysisRule
    {
        public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
        {
            ArgumentNullException.ThrowIfNull(analysis);

            var query = analysis.Pair.Statement.TopLevelQuery;

            if (query is null || query.IsSetOperation || !query.HasOrderBy)
                return Array.Empty<CombinedAnalysisFinding>();

            var sort = PlanShape.TopSort(analysis.Pair.Plan.Root);

            if (sort is null)
                return Array.Empty<CombinedAnalysisFinding>();

            var planFinding = analysis.PlanFindings.FirstOrDefault(finding =>
                finding.RuleId == "SORT_SPILL" && ReferenceEquals(finding.Node, sort));

            if (planFinding is null)
                return Array.Empty<CombinedAnalysisFinding>();

            return new[]
            {
                new CombinedAnalysisFinding
                {
                    RuleId = "ORDER_BY_WITH_SORT_SPILL",
                    Title = "Az ORDER BY rendezése lemezterületet használt",
                    SqlObservation = "Az SQL felső szintű lekérdezése ORDER BY rendezést tartalmaz.",
                    PlanObservation = planFinding.Description,
                    Description = "A párosított terv felső szintű rendezése lemezterületet is használt. " +
                        "Ez összekapcsolja az SQL rendezési igényét a tervben megfigyelt lemezhasználattal, " +
                        "de önmagában nem bizonyít teljesítményproblémát.",
                    Recommendation = planFinding.Recommendation,
                    RelatedPlanFinding = planFinding
                }
            };
        }

    }
}
