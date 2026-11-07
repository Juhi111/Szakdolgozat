using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

/// <summary>Azonos táblán és AST-részkifejezésen alapuló, közös szűrési összekapcsolás.</summary>
public abstract class PredicateWithHighRemovalRule : ICombinedAnalysisRule
{
    private readonly string sqlRuleId;
    private readonly string combinedRuleId;
    private readonly string title;
    private readonly string recommendation;

    protected PredicateWithHighRemovalRule(string sqlRuleId, string combinedRuleId, string title, string recommendation)
    {
        this.sqlRuleId = sqlRuleId;
        this.combinedRuleId = combinedRuleId;
        this.title = title;
        this.recommendation = recommendation;
    }

    public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var query = analysis.Pair.Statement.TopLevelQuery;
        if (query is null || query.IsSetOperation || !query.HasOnlyDirectRelations || query.Relations.Count != 1)
            return Array.Empty<CombinedAnalysisFinding>();
        var node = PlanShape.SingleRelationScan(analysis.Pair.Plan.Root);
        if (node is null || !PlanFilterMatcher.MatchesRelation(node, query.Relations[0]))
            return Array.Empty<CombinedAnalysisFinding>();
        var finding = analysis.PlanFindings.FirstOrDefault(f =>
            f.RuleId == "HIGH_FILTER_REMOVAL" && ReferenceEquals(f.Node, node));
        if (finding is null || !query.Predicates.Any(p => p.RuleId == sqlRuleId && node.FilterExpressionSignatures.Contains(p.Signature)))
            return Array.Empty<CombinedAnalysisFinding>();
        return new[]
        {
            new CombinedAnalysisFinding
            {
                RuleId = combinedRuleId,
                Title = title,
                SqlObservation = $"A(z) {query.Relations[0].RelationName} tábla felső szintű WHERE feltételében szereplő kifejezés a terv szűrésében is megtalálható.",
                PlanObservation = finding.Description,
                Description = "Az SQL-ben felismert kifejezés szerkezetileg egyezik az ugyanazon táblát olvasó művelet Filter mezőjének egy részkifejezésével. " +
                    "A művelet sok sort szűrt ki. Több feltételnél ez az összesített szűrés eredménye, nem az egyes feltételek külön mért hatása.",
                Recommendation = recommendation,
                RelatedPlanFinding = finding
            }
        };
    }
}
