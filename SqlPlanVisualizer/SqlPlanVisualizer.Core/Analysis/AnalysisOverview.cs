using SqlPlanVisualizer.Core.Models.History;
namespace SqlPlanVisualizer.Core.Analysis;

public sealed record OverviewEvidence(string Label, string Description, string Recommendation);
public sealed record OverviewItem(string Title, string Brief, string NextStep)
{
    public List<OverviewEvidence> Evidence { get; } = new();
    public IReadOnlyList<string> DetailRecommendations => Evidence.Select(e => e.Recommendation.Trim())
        .Where(text => text.Length > 0 && text != NextStep.Trim()).Distinct().ToArray();
    public string EvidenceHeader => $"Megfigyelések és feltételek ({Evidence.Count})";
}

/// <summary>Közös, veszteségmentes megjelenítési modell a felület és a PDF számára.</summary>
public static class AnalysisOverview
{
    public static IReadOnlyList<OverviewItem> Create(AnalysisHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var result = new List<OverviewItem>();
        var sqlGroups = new Dictionary<string, OverviewItem>();
        foreach (var group in entry.SqlFindings.GroupBy(f => f.RuleId))
        {
            var first = group.First();
            var item = new OverviewItem(first.Title, first.Brief, first.NextStep);
            foreach (var finding in UniqueEvidence(group))
                item.Evidence.Add(new("SQL-megfigyelés", finding.Description, finding.Recommendation));
            sqlGroups[group.Key] = item;
            result.Add(item);
        }

        var covered = new HashSet<HistoryFinding>();
        foreach (var group in entry.CombinedFindings.GroupBy(f => f.RuleId))
        {
            var first = group.First();
            var item = first.RelatedSqlRuleId is { } sqlRule && sqlGroups.TryGetValue(sqlRule, out var sqlItem)
                ? sqlItem : new OverviewItem(first.Title, first.Brief, first.NextStep);
            if (!result.Contains(item)) result.Add(item);
            foreach (var finding in group)
            {
                // Régi előzménynél csak egyértelmű, egyetlen megfigyelést fedhet le a szöveges egyezés.
                var matches = entry.PlanFindings.Where(p => finding.RelatedPlanKey is { } key
                    ? p.EvidenceKey == key
                    : !string.IsNullOrWhiteSpace(finding.PlanObservation) && p.Description == finding.PlanObservation).ToArray();
                if (finding.RelatedPlanKey is not null || matches.Length == 1)
                    foreach (var match in matches) covered.Add(match);
                var exactMatches = finding.RelatedPlanKey is not null || matches.Length == 1 ? matches : [];
                var detail = string.Join("\n\n", new[] { finding.SqlObservation, finding.PlanObservation, finding.Description }
                    .Concat(exactMatches.Select(p => p.Description)).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
                var recommendation = string.Join("\n\n", new[] { finding.Recommendation }
                    .Concat(exactMatches.Select(p => p.Recommendation)).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
                item.Evidence.Add(new(NodeLabel(finding, "Kapcsolódó tervmegfigyelés"), detail, recommendation));
            }
        }

        foreach (var group in entry.PlanFindings.Where(f => !covered.Contains(f)).GroupBy(f => f.RuleId))
        {
            var first = group.First();
            var item = new OverviewItem(first.Title, first.Brief, first.NextStep);
            foreach (var finding in UniqueEvidence(group))
                item.Evidence.Add(new(NodeLabel(finding, "Tervmegfigyelés"), finding.Description, finding.Recommendation));
            result.Add(item);
        }
        return result;
    }

    // Azonos típusú, de más csomóponton/forráspozíción talált megfigyelés nem vész el.
    // Ismeretlen azonosítónál sem törlünk pusztán azonos szöveg alapján.
    private static IEnumerable<HistoryFinding> UniqueEvidence(IEnumerable<HistoryFinding> findings)
    {
        var seen = new HashSet<(string Key, string Description, string Recommendation)>();
        foreach (var finding in findings)
            if (finding.EvidenceKey is null || seen.Add((finding.EvidenceKey, finding.Description, finding.Recommendation))) yield return finding;
    }

    private static string NodeLabel(HistoryFinding finding, string prefix)
    {
        string node = string.Join(" · ", new[] { finding.NodeType, finding.RelationName,
            finding.NodePath is null ? null : $"tervpozíció: {finding.NodePath}" }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrEmpty(node) ? prefix : $"{prefix}: {node}";
    }

    public static string Describe(AnalysisHistoryEntry entry)
    {
        int count = Create(entry).Count;
        return count == 0 ? "A jelenlegi szabályok nem jeleztek vizsgálandó mintát. Ez önmagában nem igazolja, hogy a lekérdezés optimális."
            : $"{count} témát érdemes átnézned. Az SQL-hez kapcsolható tervadatokat a javaslat részleteinél találod; az önálló tervmegfigyeléseket is megtartottuk.";
    }
}
