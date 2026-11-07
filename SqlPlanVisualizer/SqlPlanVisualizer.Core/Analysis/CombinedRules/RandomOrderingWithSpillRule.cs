using System.Text.Json;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public class RandomOrderingWithSpillRule : ICombinedAnalysisRule
{
    public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var query = analysis.Pair.Statement.TopLevelQuery;
        if (query is null || query.IsSetOperation || !query.OrdersByRandom) return Array.Empty<CombinedAnalysisFinding>();
        var sort = PlanShape.TopSort(analysis.Pair.Plan.Root);
        if (sort is null || !sort.AdditionalProperties.TryGetValue("Sort Key", out var keys) || keys.ValueKind != JsonValueKind.Array ||
            !keys.EnumerateArray().Any(k => k.ValueKind == JsonValueKind.String && k.GetString()?.Trim() is "random()" or "(random())"))
            return Array.Empty<CombinedAnalysisFinding>();
        var finding = analysis.PlanFindings.FirstOrDefault(f => f.RuleId == "SORT_SPILL" && ReferenceEquals(f.Node, sort));
        if (finding is null) return Array.Empty<CombinedAnalysisFinding>();
        return new[]
        {
            new CombinedAnalysisFinding
            {
                RuleId = "RANDOM_ORDERING_WITH_SPILL",
                Title = "Véletlen rendezés lemezhasználattal",
                SqlObservation = "A felső szintű ORDER BY random() rendezést tartalmaz.",
                PlanObservation = finding.Description,
                Description = "A terv felső szintű random() rendezése lemezterületet is használt. A sorlimit önmagában nem szünteti meg a rendezési bemenet feldolgozását.",
                Recommendation = "Pontosítsd a mintavétel követelményeit. Más mintavételi eljárás eltérő eloszlást vagy elemszámot adhat, ezért nem automatikus helyettesítő. Ellenőrizd a futási időt és a bemenet méretét.",
                RelatedPlanFinding = finding
            }
        };
    }
}
