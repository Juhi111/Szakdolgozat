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
                    Title = "Az ORDER BY rendezéshez lemezterület is kellett",
                    SqlObservation = "Az SQL fő lekérdezése ORDER BY feltétellel kéri az eredmény rendezését.",
                    PlanObservation = planFinding.Description,
                    Description = "A párosított terv ehhez tartozó felső szintű rendezése nem kizárólag memóriában dolgozott: lemezterületet is használt. " +
                        "Így a kért sorrendhez további olvasás és írás társulhat. A rendezés futási ideje alapján ítélhető meg, mekkora szerepe " +
                        "van ennek a teljes lekérdezésben.",
                    Recommendation = planFinding.Recommendation,
                    RelatedPlanFinding = planFinding
                }
            };
        }

    }
}
