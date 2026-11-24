using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

/// <summary>Egy elemzés lekért adatai. A szabályok ezt olvassák új adatbázis-kérés helyett.</summary>
public sealed class AnalysisMetadataSnapshotReader : IDatabaseIndexMetadataService, IDatabaseMetadataService
{
    private readonly DatabaseConnectionOptions _source;
    private readonly IReadOnlyDictionary<DatabaseTableReference, DatabaseTableMetadata?> _tables;
    public IReadOnlyList<string> Errors { get; }
    public DateTimeOffset CapturedAt { get; }
    public AnalysisMetadataSnapshotReader(DatabaseConnectionOptions source, IReadOnlyDictionary<DatabaseTableReference, DatabaseTableMetadata?> tables, IReadOnlyList<string>? errors = null, DateTimeOffset? capturedAt = null)
    { CapturedAt = capturedAt ?? DateTimeOffset.Now; _source = source.Copy(); _tables = new Dictionary<DatabaseTableReference, DatabaseTableMetadata?>(tables); Errors = errors?.ToArray() ?? []; }
    private void Validate(DatabaseConnectionOptions options)
    {
        if (options.Host != _source.Host || options.Port != _source.Port || options.Database != _source.Database || options.Schema != _source.Schema || options.Username != _source.Username)
            throw new InvalidOperationException("A kapcsolat megváltozott. Indíts új elemzést az új adatbázishoz.");
    }
    public Task<DatabaseTableMetadata?> GetTableAsync(DatabaseConnectionOptions options, string password, DatabaseTableReference table, CancellationToken cancellationToken = default)
    {
        Validate(options); cancellationToken.ThrowIfCancellationRequested();
        if (_tables.TryGetValue(table, out var value)) return Task.FromResult(value);
        return Task.FromResult(_tables.Values.FirstOrDefault(t => t is not null && table.Schema is not null && t.Schema == table.Schema && t.Table == table.Table));
    }
    public async Task<DatabaseColumnMetadata?> GetColumnAsync(DatabaseConnectionOptions options, string password, DatabaseColumnReference column, CancellationToken cancellationToken = default)
    {
        var table = await GetTableAsync(options, password, new(column.Schema, column.Table), cancellationToken);
        var item = table?.Columns.FirstOrDefault(c => c.Name == column.Column);
        return table is null || item is null ? null : new(table.Schema, table.Table, item.Name, item.TypeSchema, item.TypeName, item.DisplayType, item.IsDomain, item.IsNullable);
    }
}
