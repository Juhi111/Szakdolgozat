using Npgsql;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public sealed class PostgreSqlConnectionService : IDatabaseConnectionService
    {
        public async Task<DatabaseConnectionInfo> TestConnectionAsync(DatabaseConnectionOptions options, string password, CancellationToken cancellationToken = default)
        {
            if(options is null)
                throw new ArgumentNullException(nameof(options));
            if(password is null)
                throw new ArgumentNullException(nameof(password));            
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Database);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Host);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Username);
            if (options.Port < 1 || options.Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(options.Port));
            cancellationToken.ThrowIfCancellationRequested();

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = options.Host,
                Port = options.Port,
                Database = options.Database,
                Username = options.Username,
                Password = password,
                Timeout = 10,
                CommandTimeout = 10,
                Pooling = false,
                ApplicationName = "SqlPlanVisualizer",
                SslMode = options.TlsMode switch
                {
                    DatabaseTlsMode.VerifyFull => SslMode.VerifyFull,
                    DatabaseTlsMode.Disable => SslMode.Disable,
                    _ => throw new ArgumentOutOfRangeException(nameof(options.TlsMode))
                }
            };
            

            await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            const string sql = "SELECT current_database(), current_user, current_setting('server_version');";

            await using var command = new NpgsqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "A kapcsolatteszt nem adott vissza szerveradatokat.");
            }

            return new DatabaseConnectionInfo
            {
                DatabaseName = reader.GetString(0),
                Username = reader.GetString(1),
                ServerVersion = reader.GetString(2)
            };
        }       
    }
}
