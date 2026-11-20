using Npgsql;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public sealed class PostgreSqlConnectionService : IDatabaseConnectionService
    {
        public async Task<DatabaseConnectionInfo> TestConnectionAsync(DatabaseConnectionOptions options, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string connectionString = PostgreSqlConnectionStringFactory.Create(options, password);

            await using var dataSource = NpgsqlDataSource.Create(connectionString);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            const string sql = "SELECT current_database(), current_user, current_setting('server_version');";

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("A kapcsolatteszt nem adott vissza szerveradatokat.");

            return new DatabaseConnectionInfo
            {
                DatabaseName = reader.GetString(0),
                Username = reader.GetString(1),
                ServerVersion = reader.GetString(2)
            };
        }
    }
}