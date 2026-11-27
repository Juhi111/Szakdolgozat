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
                RelatedSqlRuleId = "ROW_FILTER_IN_HAVING",
                Title = "Sok csoport kiesett a HAVING szűrésekor",
                SqlObservation = "A fő lekérdezés HAVING feltétele egy csoportosító oszlopot hasonlít konstanshoz, nem egy összesített értéket vizsgál.",
                PlanObservation = finding.Description,
                Description = "Az összesítést végző művelet Filter mezőjében is szerepel ez a feltétel, és a művelet sok elkészült csoportot szűrt ki. " +
                    "Az eldobott sorszám itt csoportokat jelent, nem az eredeti tábla sorait. Érdemes ellenőrizni, elkerülhető-e a később " +
                    "kieső csoportok előállítása.",
                Recommendation = "Vizsgáld meg, áttehető-e a csoportosító oszlop feltétele a WHERE részbe az eredmény megőrzésével. Az összesített " +
                    "értékek feltételei maradjanak a HAVING részben. Hasonlítsd össze a terveket és a futási időket; a PostgreSQL egyes " +
                    "esetekben már magától előrehozza ezt a szűrést.",
                RelatedPlanFinding = finding
            }
        };
    }
}
