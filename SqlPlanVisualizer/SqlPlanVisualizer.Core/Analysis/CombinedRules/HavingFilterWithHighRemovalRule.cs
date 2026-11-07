using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public class HavingFilterWithHighRemovalRule : ICombinedAnalysisRule
{
    public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var query = analysis.Pair.Statement.TopLevelQuery;
        if (query is null || query.IsSetOperation || !query.HasGroupBy || query.HasDistinct ||
            !query.HasOnlyDirectRelations || query.Relations.Count != 1) return Array.Empty<CombinedAnalysisFinding>();
        PlanNode? aggregate = analysis.Pair.Plan.Root;
        if (aggregate.NodeType == "Limit") aggregate = PlanShape.OuterChild(aggregate);
        if (query.HasOrderBy && aggregate?.NodeType == "Sort") aggregate = PlanShape.OuterChild(aggregate);
        if (aggregate?.NodeType != "Aggregate") return Array.Empty<CombinedAnalysisFinding>();
        var input = PlanShape.OuterChild(aggregate);
        var scan = input is null ? null : PlanShape.SingleRelationScan(input);
        if (scan is null || !PlanFilterMatcher.MatchesRelation(scan, query.Relations[0])) return Array.Empty<CombinedAnalysisFinding>();
        var finding = analysis.PlanFindings.FirstOrDefault(f => f.RuleId == "HIGH_FILTER_REMOVAL" && ReferenceEquals(f.Node, aggregate));
        if (finding is null || !query.Predicates.Any(p => p.RuleId == "ROW_FILTER_IN_HAVING" && aggregate.FilterExpressionSignatures.Contains(p.Signature)))
            return Array.Empty<CombinedAnalysisFinding>();
        return new[]
        {
            new CombinedAnalysisFinding
            {
                RuleId = "HAVING_FILTER_WITH_HIGH_REMOVAL",
                Title = "Csoportkulcs szűrése sok eldobott csoporttal",
                SqlObservation = "A felső szintű HAVING a csoportkulcsot konstanssal hasonlítja össze.",
                PlanObservation = finding.Description,
                Description = "Az aggregálás Filter mezőjében ugyanaz a feltétel szerepel, és az aggregáló művelet sok eredménycsoportot dobott el. Ez a szám csoportokra, nem a tábla eredeti soraira vonatkozik.",
                Recommendation = "Ellenőrizd, áthelyezhető-e a csoportkulcs feltétele a WHERE részbe az eredmény megváltoztatása nélkül. Hasonlítsd össze a terveket; az optimalizáló ezt az áthelyezést egyes esetekben már elvégzi.",
                RelatedPlanFinding = finding
            }
        };
    }
}
