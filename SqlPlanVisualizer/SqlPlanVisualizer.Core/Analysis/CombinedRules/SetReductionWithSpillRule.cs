using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

/// <summary>A felső szintű duplikációeltávolítás vagy csoportosítás lemezhasználatát kapcsolja össze.</summary>
public class SetReductionWithSpillRule : ICombinedAnalysisRule
{
    public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var query = analysis.Pair.Statement.TopLevelQuery;
        if (query is null) return Array.Empty<CombinedAnalysisFinding>();
        string? operation = query.IsUnionDistinct ? "UNION" : query.IsSetOperation ? null :
            query.HasDistinct && !query.HasGroupBy ? "DISTINCT" :
            query.HasGroupBy && !query.HasDistinct ? "GROUP BY" : null;
        if (operation is null) return Array.Empty<CombinedAnalysisFinding>();

        PlanNode? node = analysis.Pair.Plan.Root;
        if (node.NodeType == "Limit") node = PlanShape.OuterChild(node);
        // A végső ORDER BY rendezés nem a duplikációeltávolítás / csoportosítás rendezése.
        if (query.HasOrderBy && node?.NodeType == "Sort") node = PlanShape.OuterChild(node);
        if (node is null) return Array.Empty<CombinedAnalysisFinding>();

        PlanNode? evidence = null;
        string planRule = "";
        var strategy = PlanFilterMatcher.ReadText(node, "Strategy");
        if (node.NodeType == "Aggregate" && strategy is "Hashed" or "Mixed")
        {
            evidence = node;
            planRule = "HASH_AGGREGATE_SPILL";
        }
        else if ((node.NodeType == "Unique" && operation != "GROUP BY") ||
                 (node.NodeType == "Aggregate" && strategy == "Sorted" && operation == "GROUP BY"))
        {
            evidence = PlanShape.OuterChild(node);
            if (evidence?.NodeType != "Sort") return Array.Empty<CombinedAnalysisFinding>();
            planRule = "SORT_SPILL";
        }
        if (evidence is null) return Array.Empty<CombinedAnalysisFinding>();
        var finding = analysis.PlanFindings.FirstOrDefault(f => f.RuleId == planRule && ReferenceEquals(f.Node, evidence));
        if (finding is null) return Array.Empty<CombinedAnalysisFinding>();
        return new[]
        {
            new CombinedAnalysisFinding
            {
                RuleId = operation.Replace(" ", "_") + "_WITH_SPILL",
                Title = operation + " művelet lemezhasználattal",
                SqlObservation = $"A felső szintű lekérdezés {operation} műveletet tartalmaz.",
                PlanObservation = finding.Description,
                Description = "A műveletet megvalósító felső szintű tervrész lemezhasználatot jelentett. Ez önmagában nem bizonyít lassulást.",
                Recommendation = (operation == "UNION" ? "UNION ALL csak akkor használható, ha az ismétlődések megtartása megengedett. " :
                    operation == "DISTINCT" ? "Ellenőrizd, szükséges-e az ismétlődések eltávolítása; a DISTINCT elhagyása megváltoztathatja az eredményt. " :
                    "Vizsgáld meg, csökkenthető-e a csoportosítás bemenete az eredmény megváltoztatása nélkül. ") + finding.Recommendation,
                RelatedPlanFinding = finding
            }
        };
    }
}
