using Npgsql;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    internal static class PostgreSqlConnectionStringFactory
    {
        public static string Create(DatabaseConnectionOptions options, string password)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(password);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Host);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Database);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Username);

            if (options.Port < 1 || options.Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(options.Port), "A port értéke 1 és 65535 között lehet.");

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

            return builder.ConnectionString;
        }
    }
}