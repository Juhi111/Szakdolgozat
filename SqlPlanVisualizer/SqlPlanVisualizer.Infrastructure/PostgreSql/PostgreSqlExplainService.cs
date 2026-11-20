using Npgsql;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public sealed class PostgreSqlExplainService : IDatabaseExplainService
    {
        private readonly ExplainQueryValidator _validator = new();

        /// <summary>
        /// Becsült végrehajtási tervet kér le.
        /// </summary>
        public Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default)
        {
            return GetPlanAsync(options, password, sql, false, cancellationToken);
        }

        /// <summary>
        /// Végrehajtja a lekérdezést, és visszaadja a mért végrehajtási tervet.
        /// </summary>
        public Task<string> GetActualPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default)
        {
            return GetPlanAsync(options, password, sql, true, cancellationToken);
        }

        private async Task<string> GetPlanAsync(DatabaseConnectionOptions options, string password, string sql, bool analyze, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(password);
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Schema);

            cancellationToken.ThrowIfCancellationRequested();
            _validator.Validate(sql);

            string schema = options.Schema;
            string connectionString = PostgreSqlConnectionStringFactory.Create(options, password);

            await using var dataSource = NpgsqlDataSource.Create(connectionString);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            const string setupSql = """
                SET TRANSACTION READ ONLY;
                SET LOCAL statement_timeout = '10s';
                SET LOCAL lock_timeout = '3s';
                """;

            await using (var setupCommand = new NpgsqlCommand(setupSql, connection, transaction))
            {
                await setupCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string schemaCheckSql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_namespace
                    WHERE nspname = $1
                      AND pg_catalog.has_schema_privilege(oid, 'USAGE')
                );
                """;

            await using (var schemaCommand = new NpgsqlCommand(schemaCheckSql, connection, transaction))
            {
                schemaCommand.Parameters.AddWithValue(schema);

                var schemaAvailable = await schemaCommand.ExecuteScalarAsync(cancellationToken);

                if (schemaAvailable is not true)
                    throw new InvalidOperationException("A megadott séma nem létezik, vagy nincs hozzá használati jogosultság.");
            }

            const string searchPathSql = """
                SELECT pg_catalog.set_config(
                    'search_path',
                    'pg_catalog, ' || pg_catalog.quote_ident($1) || ', pg_temp',
                    true
                );
                """;

            await using (var schemaCommand = new NpgsqlCommand(searchPathSql, connection, transaction))
            {
                schemaCommand.Parameters.AddWithValue(schema);
                await schemaCommand.ExecuteScalarAsync(cancellationToken);
            }

            string explainPrefix = analyze ? "EXPLAIN (ANALYZE, BUFFERS, VERBOSE, SETTINGS, FORMAT JSON)" : "EXPLAIN (VERBOSE, FORMAT JSON)";

            string explainSql = explainPrefix + "\n" + sql;

            await using var explainCommand = new NpgsqlCommand(explainSql, connection, transaction);
            var result = await explainCommand.ExecuteScalarAsync(cancellationToken);

            if (result is not string json || string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("A tervlekérés nem adott vissza EXPLAIN JSON-t.");

            await transaction.RollbackAsync(cancellationToken);

            return json;
        }
    }
}