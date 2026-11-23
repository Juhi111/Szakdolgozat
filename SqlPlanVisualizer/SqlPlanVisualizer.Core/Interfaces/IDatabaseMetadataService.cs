using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Core.Interfaces;

public interface IDatabaseMetadataService
{
    Task<DatabaseColumnMetadata?> GetColumnAsync(DatabaseConnectionOptions options, string password,
        DatabaseColumnReference column, CancellationToken cancellationToken = default);
}
