using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Core.Interfaces
{
    public interface IDatabaseConnectionProfileService
    {
        Task<List<DatabaseConnectionProfile>> LoadAsync();
        Task SaveAsync(DatabaseConnectionProfile profile);
        Task DeleteAsync(Guid id);
    }
}
