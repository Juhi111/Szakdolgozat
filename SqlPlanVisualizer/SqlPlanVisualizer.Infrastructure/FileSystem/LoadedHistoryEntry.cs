using SqlPlanVisualizer.Core.Models.History;

namespace SqlPlanVisualizer.Infrastructure.FileSystem;

/// <summary>Egy visszaolvasott előzmény és az azt tartalmazó fájl útvonala.</summary>
public sealed record LoadedHistoryEntry(string FilePath, AnalysisHistoryEntry Entry);
