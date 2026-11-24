namespace SqlPlanVisualizer.Core.Models.Database;

public sealed record DatabaseTableReference(string? Schema, string Table);

public sealed class DatabaseIndexElement
{
    public int Position { get; init; }
    public string? Column { get; init; }
    public string Definition { get; init; } = "";
    public bool Included { get; init; }
    public bool Descending { get; init; }
    public bool NullsFirst { get; init; }
}

public sealed class DatabaseIndexMetadata
{
    public string Name { get; init; } = "";
    public string Method { get; init; } = "";
    public string Definition { get; init; } = "";
    public string? Predicate { get; init; }
    public bool IsUnique { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsValid { get; init; }
    public bool IsReady { get; init; }
    public bool IsLive { get; init; }
    public bool NullsNotDistinct { get; init; }
    public List<DatabaseIndexElement> Elements { get; init; } = new();
    public bool IsUsable => IsValid && IsReady && IsLive;
    public string KeysText => string.Join(", ", Elements.Where(e => !e.Included).OrderBy(e => e.Position).Select(e => e.Definition));
    public string IncludedText => string.Join(", ", Elements.Where(e => e.Included).OrderBy(e => e.Position).Select(e => e.Definition));
    public string StateText => $"{Method}; {(IsPrimary ? "elsődleges kulcs" : IsUnique ? "egyedi index" : "nem egyedi index")}; " +
        $"érvényes: {(IsValid ? "igen" : "nem")}; írásra kész: {(IsReady ? "igen" : "nem")}; élő: {(IsLive ? "igen" : "nem")}; NULLS NOT DISTINCT: {(NullsNotDistinct ? "igen" : "nem")}";
}

public sealed class DatabaseKeyMetadata
{
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Definition { get; init; } = "";
    public string? IndexName { get; init; }
    public bool Deferrable { get; init; }
    public bool InitiallyDeferred { get; init; }
    public bool Validated { get; init; }
    public List<string> Columns { get; init; } = new();
    public string Details => $"{Name}: {Definition}; validált: {(Validated ? "igen" : "nem")}; halasztható: {(Deferrable ? "igen" : "nem")}; kezdetben halasztott: {(InitiallyDeferred ? "igen" : "nem")}";
}

public sealed class DatabaseTableMetadata
{
    public string Schema { get; init; } = "";
    public string Table { get; init; } = "";
    public List<DatabaseIndexMetadata> Indexes { get; init; } = new();
    public List<DatabaseKeyMetadata> Keys { get; init; } = new();
    public List<ColumnAnalysisMetadata> Columns { get; init; } = new();
    public List<ColumnStatisticsMetadata> ColumnStatistics { get; init; } = new();
    public TableStatisticsMetadata Statistics { get; init; } = new();
    public List<string> ExtendedStatistics { get; init; } = new();
    public Dictionary<string, string?> Settings { get; set; } = new();
    public List<string> Extensions { get; set; } = new();
    public string DisplayName => $"{Schema}.{Table}";
}

public sealed record IndexObservation(string Title, string Description);
public sealed record TableIndexReport(DatabaseTableMetadata Metadata, IReadOnlyList<IndexObservation> Observations);
public sealed record IndexMetadataReport(DateTimeOffset CapturedAt, string Source, IReadOnlyList<TableIndexReport> Tables, IReadOnlyList<string> Notices)
{
    public string SourceText => $"{Source}; metaadatok lekérve: {CapturedAt:yyyy.MM.dd. HH:mm:ss zzz}. A terv korábbi pillanatkép; az indexek azóta változhattak.";
}
