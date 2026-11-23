using Npgsql;
using System.Text.Json;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

/// <summary>Az érintett tábla indexeit és PK/UNIQUE megszorításait olvassa. Nem futtat DDL-t.</summary>
public sealed class PostgreSqlIndexMetadataService : IDatabaseIndexMetadataService
{
    public async Task<DatabaseTableMetadata?> GetTableAsync(DatabaseConnectionOptions options, string password,
        DatabaseTableReference table, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(table.Table);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Schema);
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
        await using var command = new NpgsqlCommand(CatalogSql, connection, transaction);
        command.Parameters.AddWithValue(PostgreSqlMetadataService.RelationName(new(table.Schema, table.Table, "")));
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json ? JsonSerializer.Deserialize<DatabaseTableMetadata>(json) : null;
    }

    // Egyetlen statement biztosítja a tábla/index/kulcs adatok közös adatbázis-pillanatképét.
    private const string CatalogSql = """
        SELECT pg_catalog.jsonb_build_object(
            'Schema', n.nspname, 'Table', c.relname,
            'Indexes', COALESCE((
                SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                    'Name', ic.relname, 'Method', am.amname,
                    'Definition', pg_catalog.pg_get_indexdef(ix.indexrelid),
                    'Predicate', pg_catalog.pg_get_expr(ix.indpred, ix.indrelid),
                    'IsUnique', ix.indisunique, 'IsPrimary', ix.indisprimary,
                    'IsValid', ix.indisvalid, 'IsReady', ix.indisready, 'IsLive', ix.indislive,
                    'NullsNotDistinct', ix.indnullsnotdistinct,
                    'Elements', (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                        'Position', k.pos, 'Column', a.attname,
                        'Definition', pg_catalog.pg_get_indexdef(ix.indexrelid, k.pos, true),
                        'Included', k.pos > ix.indnkeyatts,
                        'Descending', CASE WHEN am.amname = 'btree' AND k.pos <= ix.indnkeyatts THEN (ix.indoption[k.pos - 1] & 1) <> 0 ELSE false END,
                        'NullsFirst', CASE WHEN am.amname = 'btree' AND k.pos <= ix.indnkeyatts THEN (ix.indoption[k.pos - 1] & 2) <> 0 ELSE false END
                    ) ORDER BY k.pos)
                    FROM pg_catalog.generate_series(1, ix.indnatts::integer) k(pos)
                    LEFT JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid AND a.attnum = ix.indkey[k.pos - 1])
                ) ORDER BY ic.relname)
                FROM pg_catalog.pg_index ix
                JOIN pg_catalog.pg_class ic ON ic.oid = ix.indexrelid
                JOIN pg_catalog.pg_am am ON am.oid = ic.relam
                WHERE ix.indrelid = c.oid
            ), '[]'::jsonb),
            'Keys', COALESCE((
                SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                    'Name', con.conname, 'Kind', con.contype::text,
                    'Definition', pg_catalog.pg_get_constraintdef(con.oid),
                    'IndexName', ci.relname, 'Deferrable', con.condeferrable,
                    'InitiallyDeferred', con.condeferred, 'Validated', con.convalidated,
                    'Columns', (SELECT pg_catalog.jsonb_agg(a.attname ORDER BY k.pos)
                        FROM pg_catalog.unnest(con.conkey) WITH ORDINALITY k(attnum, pos)
                        JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid AND a.attnum = k.attnum)
                ) ORDER BY con.conname)
                FROM pg_catalog.pg_constraint con
                LEFT JOIN pg_catalog.pg_class ci ON ci.oid = con.conindid
                WHERE con.conrelid = c.oid AND con.contype IN ('p', 'u')
            ), '[]'::jsonb)
        )::text
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE c.oid = pg_catalog.to_regclass($1) AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
          AND pg_catalog.has_schema_privilege(n.oid, 'USAGE')
          AND (pg_catalog.has_table_privilege(c.oid, 'SELECT') OR pg_catalog.has_any_column_privilege(c.oid, 'SELECT'));
        """;
}
