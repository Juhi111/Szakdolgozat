using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public class LargeOffsetWithLargeSortRule : ICombinedAnalysisRule
{
    public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var query = analysis.Pair.Statement.TopLevelQuery;
        var root = analysis.Pair.Plan.Root;
        if (query is null || query.IsSetOperation || query.OffsetSourceOffset is not int offset || root.NodeType != "Limit")
            return Array.Empty<CombinedAnalysisFinding>();
        var sql = analysis.SqlFindings.FirstOrDefault(f => f.RuleId == "LARGE_OFFSET" && f.SourceOffset == offset);
        var plan = analysis.PlanFindings.FirstOrDefault(f => f.RuleId == "LARGE_SORT_FOR_LIMIT" && ReferenceEquals(f.Node, root));
        if (sql is null || plan is null) return Array.Empty<CombinedAnalysisFinding>();
        return new[]
        {
            new CombinedAnalysisFinding
            {
                RuleId = "LARGE_OFFSET_WITH_LARGE_SORT",
                Title = "Nagy OFFSET mellett nagy rendezési bemenet",
                SqlObservation = sql.Description,
                PlanObservation = plan.Description,
                Description = "A felső szintű lapozás nagy eltolást tartalmaz, és ugyanazon Limit tervrész alatt nagy rendezési bemenet figyelhető meg. A mérés nem különíti el az OFFSET önálló költségét.",
                Recommendation = sql.Recommendation + " " + plan.Recommendation,
                RelatedPlanFinding = plan
            }
        };
    }
}
