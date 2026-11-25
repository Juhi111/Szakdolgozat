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
        var snapshot = await LoadSnapshotAsync(options, password, new[] { table }, cancellationToken);
        return await snapshot.GetTableAsync(options, password, table, cancellationToken);
    }

    public async Task<AnalysisMetadataSnapshotReader> LoadSnapshotAsync(DatabaseConnectionOptions options, string password,
        IEnumerable<DatabaseTableReference> tables, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options); ArgumentNullException.ThrowIfNull(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Schema);
        var snapshot = options.Copy();
        var references = tables.Distinct().Take(64).ToArray();
        await using var source = NpgsqlDataSource.Create(PostgreSqlConnectionStringFactory.Create(snapshot, password));
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        await using (var setup = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL statement_timeout = '10s'; SET LOCAL lock_timeout = '3s';", connection, transaction))
            await setup.ExecuteNonQueryAsync(cancellationToken);
        await using (var path = new NpgsqlCommand("SELECT pg_catalog.set_config('search_path', 'pg_catalog, ' || pg_catalog.quote_ident($1) || ', pg_temp', true)", connection, transaction))
        {
            path.Parameters.AddWithValue(snapshot.Schema); await path.ExecuteScalarAsync(cancellationToken);
        }
        const string settingsSql = "SELECT jsonb_object_agg(name, current_setting(name, true))::text FROM unnest(ARRAY['server_version','work_mem','hash_mem_multiplier','max_parallel_workers_per_gather','max_parallel_workers','max_worker_processes','effective_cache_size','random_page_cost','seq_page_cost','default_statistics_target','TimeZone','DateStyle','track_counts','track_io_timing']) AS setting(name)";
        await using var settingsCommand = new NpgsqlCommand(settingsSql, connection, transaction);
        var settings = JsonSerializer.Deserialize<Dictionary<string, string?>>((string)(await settingsCommand.ExecuteScalarAsync(cancellationToken))!)!;
        await using var extensionsCommand = new NpgsqlCommand("SELECT COALESCE(jsonb_agg(extname::text), '[]'::jsonb)::text FROM pg_catalog.pg_extension", connection, transaction);
        var extensions = JsonSerializer.Deserialize<List<string>>((string)(await extensionsCommand.ExecuteScalarAsync(cancellationToken))!)!;
        var data = new Dictionary<DatabaseTableReference, DatabaseTableMetadata?>();
        var errors = new List<string>();
        foreach (var reference in references)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.SaveAsync("metadata_table", cancellationToken);
            try
            {
                await using var command = new NpgsqlCommand(CatalogSql, connection, transaction);
                command.Parameters.AddWithValue(PostgreSqlMetadataService.RelationName(new(reference.Schema, reference.Table, "")));
                var value = await command.ExecuteScalarAsync(cancellationToken);
                var table = value is string json ? JsonSerializer.Deserialize<DatabaseTableMetadata>(json) : null;
                if (table is not null) { table.Settings = settings; table.Extensions = extensions; }
                data[reference] = table;
                await transaction.ReleaseAsync("metadata_table", cancellationToken);
            }
            catch (Exception ex) when (ex is PostgresException or JsonException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await transaction.RollbackAsync("metadata_table", cancellationToken);
                data[reference] = null; errors.Add($"{reference.Schema}.{reference.Table}: az adatbázisadatok nem érhetők el. Az elemzés ezek nélkül folytatódik.");
            }
        }
        return new(snapshot, data, errors);
    }

    // Egyetlen statement biztosítja a tábla/index/kulcs adatok közös adatbázis-pillanatképét.
    private const string CatalogSql = """
        SELECT pg_catalog.jsonb_build_object(
             'Schema', n.nspname, 'Table', c.relname,
             'RelationKind', c.relkind::text, 'RowSecurity', c.relrowsecurity,
             'HasDescendants', EXISTS (SELECT 1 FROM pg_inherits WHERE inhparent = c.oid),
            'Columns', COALESCE((SELECT jsonb_agg(jsonb_build_object(
                'Name', a.attname, 'TypeSchema', tn.nspname, 'TypeName', t.typname,
                'DisplayType', format_type(a.atttypid, a.atttypmod), 'IsDomain', t.typtype = 'd',
                'IsNullable', CASE WHEN t.typtype = 'd' THEN NULL
                    WHEN a.attnotnull AND EXISTS (SELECT 1 FROM pg_constraint nn WHERE nn.conrelid = c.oid
                        AND nn.contype = 'n' AND a.attnum = ANY(nn.conkey) AND NOT nn.convalidated) THEN NULL
                    ELSE NOT a.attnotnull END,
                'Collation', cn.nspname || '.' || coll.collname, 'Generated', a.attgenerated::text,
                'Identity', a.attidentity::text, 'StatisticsTarget', a.attstattarget
            ) ORDER BY a.attnum) FROM pg_attribute a JOIN pg_type t ON t.oid = a.atttypid
              JOIN pg_namespace tn ON tn.oid = t.typnamespace
              LEFT JOIN pg_collation coll ON coll.oid = a.attcollation LEFT JOIN pg_namespace cn ON cn.oid = coll.collnamespace
              WHERE a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped AND has_column_privilege(c.oid, a.attnum, 'SELECT')), '[]'::jsonb),
            'ColumnStatistics', COALESCE((SELECT jsonb_agg(jsonb_build_object(
                'Column', st.attname, 'Inherited', st.inherited, 'NullFraction', st.null_frac,
                'DistinctEstimate', st.n_distinct, 'AverageWidth', st.avg_width, 'Correlation', st.correlation
            )) FROM pg_stats st WHERE st.schemaname = n.nspname AND st.tablename = c.relname), '[]'::jsonb),
            'Statistics', jsonb_build_object('EstimatedRows', CASE WHEN c.reltuples >= 0 THEN c.reltuples ELSE NULL END,
                'TotalBytes', pg_total_relation_size(c.oid), 'Pages', c.relpages, 'AllVisiblePages', c.relallvisible,
                'LiveRows', stat.n_live_tup, 'DeadRows', stat.n_dead_tup, 'ModifiedSinceAnalyze', stat.n_mod_since_analyze,
                'LastAnalyze', stat.last_analyze, 'LastAutoAnalyze', stat.last_autoanalyze,
                'LastVacuum', stat.last_vacuum, 'LastAutoVacuum', stat.last_autovacuum),
            'ExtendedStatistics', COALESCE((SELECT jsonb_agg(pg_get_statisticsobjdef(es.oid)) FROM pg_statistic_ext es WHERE es.stxrelid = c.oid), '[]'::jsonb),
            'Indexes', COALESCE((
                SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                    'Name', ic.relname, 'Method', am.amname,
                    'Definition', pg_catalog.pg_get_indexdef(ix.indexrelid),
                    'Predicate', pg_catalog.pg_get_expr(ix.indpred, ix.indrelid),
                    'IsUnique', ix.indisunique, 'IsPrimary', ix.indisprimary,
                    'IsValid', ix.indisvalid, 'IsReady', ix.indisready, 'IsLive', ix.indislive,
                    'NullsNotDistinct', ix.indnullsnotdistinct,
                    'HasDefaultOperatorClasses', (SELECT bool_and(opc.opcdefault) FROM unnest(ix.indclass) cls(oid) JOIN pg_opclass opc ON opc.oid = cls.oid),
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
                WHERE con.conrelid = c.oid AND con.contype IN ('p', 'u', 'f', 'c')
            ), '[]'::jsonb)
        )::text
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        LEFT JOIN pg_catalog.pg_stat_all_tables stat ON stat.relid = c.oid
        WHERE c.oid = pg_catalog.to_regclass($1) AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
          AND pg_catalog.has_schema_privilege(n.oid, 'USAGE')
          AND (pg_catalog.has_table_privilege(c.oid, 'SELECT') OR pg_catalog.has_any_column_privilege(c.oid, 'SELECT'));
        """;
}
