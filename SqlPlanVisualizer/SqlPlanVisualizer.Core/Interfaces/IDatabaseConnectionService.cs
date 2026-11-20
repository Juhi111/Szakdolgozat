using SqlPlanVisualizer.Core.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Interfaces
{
    public interface IDatabaseConnectionService
    {
        Task<DatabaseConnectionInfo> TestConnectionAsync(DatabaseConnectionOptions options, string password, CancellationToken cancellationToken = default);
    }
}
