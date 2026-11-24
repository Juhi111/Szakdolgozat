namespace SqlPlanVisualizer.Core.Models.Database;

public sealed class ColumnAnalysisMetadata
{
    public string Name { get; init; } = "";
    public string TypeSchema { get; init; } = "";
    public string TypeName { get; init; } = "";
    public string DisplayType { get; init; } = "";
    public bool IsDomain { get; init; }
    public bool? IsNullable { get; init; }
    public string? Collation { get; init; }
    public string Generated { get; init; } = "";
    public string Identity { get; init; } = "";
    public int? StatisticsTarget { get; init; }
    public string Description => $"{Name}: {DisplayType}; NULL: {(IsNullable is null ? "ismeretlen" : IsNullable.Value ? "megengedett" : "tiltott")}; kolláció: {Collation ?? "nincs"}";
}
public sealed record ColumnStatisticsMetadata(string Column, bool Inherited, double? NullFraction, double? DistinctEstimate, int? AverageWidth, double? Correlation);
public sealed class TableStatisticsMetadata
{
    public double? EstimatedRows { get; init; }
    public long? TotalBytes { get; init; }
    public int? Pages { get; init; }
    public int? AllVisiblePages { get; init; }
    public long? LiveRows { get; init; }
    public long? DeadRows { get; init; }
    public long? ModifiedSinceAnalyze { get; init; }
    public DateTimeOffset? LastAnalyze { get; init; }
    public DateTimeOffset? LastAutoAnalyze { get; init; }
    public DateTimeOffset? LastVacuum { get; init; }
    public DateTimeOffset? LastAutoVacuum { get; init; }
    private static string Number(object? value) => value is null ? "nincs adat" : Convert.ToString(value, System.Globalization.CultureInfo.GetCultureInfo("hu-HU"))!;
    private static string Date(DateTimeOffset? value) => value?.ToString("yyyy.MM.dd. HH:mm zzz") ?? "nincs adat";
    public string Description => $"Becsült sorok: {Number(EstimatedRows)}; teljes méret: {Number(TotalBytes)} bájt; becsült elhalt sorok: {Number(DeadRows)}; ANALYZE óta módosított sorok: {Number(ModifiedSinceAnalyze)}. " +
        $"Utolsó ANALYZE: {Date(LastAnalyze)}; automatikus ANALYZE: {Date(LastAutoAnalyze)}; VACUUM: {Date(LastVacuum)}; automatikus VACUUM: {Date(LastAutoVacuum)}. A hiányzó időpont nem bizonyítja a karbantartás hiányát.";
}
