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
                Title = "A véletlen rendezés lemezterületet is használt",
                SqlObservation = "A fő lekérdezés ORDER BY random() kifejezéssel kér véletlen sorrendet.",
                PlanObservation = finding.Description,
                Description = "A párosított terv felső szintű rendezési kulcsai között is szerepel a random(), és a rendezés lemezterületet használt. A " +
                    "véletlen sorrend előállítása így lemezműveletekkel is járhatott. Egy sorlimit önmagában nem szünteti meg a rendezés " +
                    "bemenetének feldolgozását.",
                Recommendation = "Ellenőrizd a rendezés bemenetét és idejét. Ha csak véletlen mintára van szükség, vizsgáld meg más mintavételi eljárás " +
                    "lehetőségét. Az elemszám, az eloszlás és a szűréshez való viszony eltérhet, ezért ezeket az elvárt eredményhez kell " +
                    "igazítani. Ha a teljes eredmény véletlen sorrendje szükséges, a lemezes rendezés elfogadható is lehet.",
                RelatedPlanFinding = finding
            }
        };
    }
}
