using SqlPlanVisualizer.Core.Models.History;
namespace SqlPlanVisualizer.Core.Analysis;

public sealed record OverviewItem(string Title, string Brief, string NextStep);
public static class AnalysisOverview
{
    public static IReadOnlyList<OverviewItem> Create(AnalysisHistoryEntry entry)
    {
        // Csak pontosan azonos tervmegfigyelést vonunk össze; más csomópont találatát nem tüntetjük el.
        var covered = entry.CombinedFindings.Select(f => f.PlanObservation).Where(s => !string.IsNullOrEmpty(s)).ToHashSet();
        return entry.CombinedFindings.Concat(entry.SqlFindings).Concat(entry.PlanFindings.Where(f => !covered.Contains(f.Description)))
            .Select(f => new OverviewItem(f.Title, f.Brief, f.NextStep)).Distinct().ToArray();
    }
    public static string Describe(AnalysisHistoryEntry entry)
    {
        int count = Create(entry).Count;
        return count == 0 ? "A jelenlegi szabályok nem jeleztek vizsgálandó mintát. Ez önmagában nem igazolja, hogy a lekérdezés optimális."
            : $"{count} megfigyelést érdemes átnézned. Nem mindegyik jelent hibát: a rövid javaslatok segítenek eldönteni, mit vizsgálj meg először.";
    }
}
