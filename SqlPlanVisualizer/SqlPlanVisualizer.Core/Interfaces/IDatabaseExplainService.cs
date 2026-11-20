using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Core.Interfaces
{
    public interface IDatabaseExplainService
    {
        Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default);
        Task<string> GetActualPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default);
    }
}