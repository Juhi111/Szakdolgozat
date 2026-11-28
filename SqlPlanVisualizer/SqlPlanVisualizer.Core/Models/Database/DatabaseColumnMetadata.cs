namespace SqlPlanVisualizer.Core.Models.Database;

public sealed record DatabaseColumnReference(string? Schema, string Table, string Column);
public enum RewriteTypeRequirement { DateOrTimestamp, Integer, Text, DateTimestampOrTimestampTz, Boolean, IntegerOrNumeric }
public sealed record RewriteMetadataRequirement(DatabaseColumnReference Column, RewriteTypeRequirement Type);

/// <summary>A katalógusból olvasott oszlopadat; domainnél a nullázhatóság ismeretlen lehet.</summary>
public sealed record DatabaseColumnMetadata(string Schema, string Table, string Column, string TypeSchema,
    string TypeName, string DisplayType, bool IsDomain, bool? IsNullable);

public enum MetadataCheckStatus { Unknown, Passed, Failed }
public sealed record RewriteMetadataCheck(MetadataCheckStatus Status, string Message, DatabaseColumnMetadata? Column = null,
    DateTimeOffset? CheckedAt = null, string? Source = null)
{
    public bool BlocksExecution => Status == MetadataCheckStatus.Failed;
    public string Description => Message + (Column is null ? "" : $" Oszlop: {Column.Schema}.{Column.Table}.{Column.Column}; típus: {Column.TypeSchema}.{Column.TypeName} ({Column.DisplayType}); NULL: {(Column.IsNullable is null ? "ismeretlen" : Column.IsNullable.Value ? "megengedett" : "nem megengedett") }.") +
        (CheckedAt is null ? "" : $" Ellenőrizve: {CheckedAt:O}; forrás: {Source}.") + " Az eredményazonosság nincs igazolva.";
    public static RewriteMetadataCheck NotChecked(string reason = "A metaadatokat még nem ellenőriztük.") => new(MetadataCheckStatus.Unknown, reason);
}
