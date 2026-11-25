using Npgsql;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

/// <summary>Csak a kért oszlop metaadatait olvassa, az EXPLAIN szolgáltatáséval azonos search_path mellett.</summary>
public sealed class PostgreSqlMetadataService : IDatabaseMetadataService
{
    public async Task<DatabaseColumnMetadata?> GetColumnAsync(DatabaseConnectionOptions options, string password,
        DatabaseColumnReference column, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(column);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(column.Table);
        ArgumentException.ThrowIfNullOrWhiteSpace(column.Column);
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = options.Copy();
        await using var source = NpgsqlDataSource.Create(PostgreSqlConnectionStringFactory.Create(snapshot, password));
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var setup = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL statement_timeout = '10s'; SET LOCAL lock_timeout = '3s';", connection, transaction))
            await setup.ExecuteNonQueryAsync(cancellationToken);
        await using (var path = new NpgsqlCommand("SELECT pg_catalog.set_config('search_path', 'pg_catalog, ' || pg_catalog.quote_ident($1) || ', pg_temp', true)", connection, transaction))
        {
            path.Parameters.AddWithValue(snapshot.Schema);
            await path.ExecuteScalarAsync(cancellationToken);
        }
        const string sql = """
            SELECT n.nspname::text, c.relname::text, a.attname::text, tn.nspname::text, t.typname::text,
                   pg_catalog.format_type(a.atttypid, a.atttypmod), t.typtype = 'd',
                   CASE WHEN t.typtype = 'd' THEN NULL::boolean
                        WHEN a.attnotnull AND EXISTS (SELECT 1 FROM pg_catalog.pg_constraint nn WHERE nn.conrelid = c.oid
                            AND nn.contype = 'n' AND a.attnum = ANY(nn.conkey) AND NOT nn.convalidated) THEN NULL::boolean
                        ELSE NOT a.attnotnull END
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid
            JOIN pg_catalog.pg_type t ON t.oid = a.atttypid
            JOIN pg_catalog.pg_namespace tn ON tn.oid = t.typnamespace
            WHERE c.oid = pg_catalog.to_regclass($1) AND a.attname = $2 AND a.attnum > 0 AND NOT a.attisdropped
              AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
              AND pg_catalog.has_schema_privilege(n.oid, 'USAGE')
              AND pg_catalog.has_column_privilege(c.oid, a.attnum, 'SELECT')
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(RelationName(column));
        command.Parameters.AddWithValue(column.Column);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
            reader.GetString(5), reader.GetBoolean(6), reader.IsDBNull(7) ? null : reader.GetBoolean(7));
    }

    public static string RelationName(DatabaseColumnReference column)
    {
        static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
        return column.Schema is null ? Quote(column.Table) : Quote(column.Schema) + "." + Quote(column.Table);
    }
}
