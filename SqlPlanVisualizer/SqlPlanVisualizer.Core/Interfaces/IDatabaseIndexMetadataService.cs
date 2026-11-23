using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Core.Interfaces;

public interface IDatabaseIndexMetadataService
{
    Task<DatabaseTableMetadata?> GetTableAsync(DatabaseConnectionOptions options, string password,
        DatabaseTableReference table, CancellationToken cancellationToken = default);
}
