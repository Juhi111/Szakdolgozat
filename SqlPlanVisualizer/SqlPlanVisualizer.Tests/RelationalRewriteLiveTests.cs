using Npgsql;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class RelationalRewriteLiveTests
{
    [PostgreSqlMetadataFact]
    public async Task LiveCatalog_ProvesRewritesWithDuplicatesNullsEmptySetsAndQuotedNames()
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SQLPLAN_METADATA_TEST_CONNECTION")!);
        string schema = "rewrite_" + Guid.NewGuid().ToString("N");
        var options = new DatabaseConnectionOptions { Host = builder.Host!, Port = builder.Port, Database = builder.Database!, Username = builder.Username!, Schema = schema, TlsMode = DatabaseTlsMode.Disable };
        await using var connection = new NpgsqlConnection(builder.ConnectionString); await connection.OpenAsync();
        async Task Execute(string sql) { await using var cmd = new NpgsqlCommand(sql, connection); await cmd.ExecuteNonQueryAsync(); }
        async Task<List<DatabaseTableMetadata>> Metadata(params string[] names)
        {
            var refs = names.Select(n => new DatabaseTableReference(schema, n)).ToArray();
            var snapshot = await new PostgreSqlIndexMetadataService().LoadSnapshotAsync(options, builder.Password ?? "", refs);
            var tables = new List<DatabaseTableMetadata>();
            foreach (var reference in refs) tables.Add((await snapshot.GetTableAsync(options, "", reference))!);
            Assert.All(tables, t => { Assert.NotNull(t); Assert.Equal("r", t.RelationKind); });
            return tables;
        }
        try
        {
            await Execute($"""
                CREATE SCHEMA {schema};
                CREATE TABLE {schema}.customers(id bigint PRIMARY KEY, name text);
                CREATE TABLE {schema}.orders(id bigint PRIMARY KEY, customer_id bigint NOT NULL);
                CREATE TABLE {schema}.nullable_orders(id bigint PRIMARY KEY, customer_id bigint);
                CREATE TABLE {schema}.duplicates(id bigint, name text);
                CREATE TABLE {schema}.composite(a bigint, b bigint, PRIMARY KEY(a,b));
                CREATE TABLE {schema}."Odd Table"("Key" bigint PRIMARY KEY);
                INSERT INTO {schema}.customers VALUES (1,'same'),(2,'same'),(3,NULL),(4,'last');
                INSERT INTO {schema}.orders VALUES(1,1),(2,1),(3,3);
                INSERT INTO {schema}.nullable_orders VALUES(1,1),(2,1),(3,NULL);
                INSERT INTO {schema}.duplicates VALUES(1,'same'),(1,'same'),(2,'other'),(NULL,'null');
                INSERT INTO {schema}.composite VALUES(1,1),(1,2),(2,1);
                INSERT INTO {schema}."Odd Table" VALUES(1),(2);
                """);
            var tables = await Metadata("customers", "orders", "nullable_orders", "duplicates", "composite", "Odd Table");
            Assert.True(tables[0].Indexes.Single(i => i.IsPrimary).HasDefaultOperatorClasses);
            var rules = new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault(tables));
            string[] cases =
            [
                RelationalRewriteTests.NotIn.Replace("lab.", schema + "."),
                RelationalRewriteTests.Anti.Replace("lab.", schema + "."),
                RelationalRewriteTests.Distinct.Replace("lab.", schema + "."),
                RelationalRewriteTests.Join.Replace("lab.", schema + "."),
                $"SELECT c.id, c.name FROM {schema}.duplicates c LEFT JOIN {schema}.nullable_orders o ON c.id=o.customer_id WHERE o.id IS NULL",
                $"SELECT DISTINCT c.a, c.b FROM {schema}.composite c",
                $"SELECT DISTINCT q.\"Key\" FROM {schema}.\"Odd Table\" q",
                $"SELECT c.id FROM {schema}.customers c WHERE c.id NOT IN (SELECT o.customer_id FROM {schema}.orders o)"
            ];
            foreach (bool emptyRight in new[] { false, true })
            {
                if (emptyRight) await Execute($"TRUNCATE {schema}.orders, {schema}.nullable_orders");
                foreach (string sql in cases)
                {
                    var rewrite = Assert.Single(rules.Suggest(sql));
                    string rewritten = rewrite.SuggestedSql.TrimEnd().TrimEnd(';');
                    // EXCEPT ALL in both directions checks both values and duplicate multiplicities.
                    await using var compare = new NpgsqlCommand($"SELECT count(*) FROM ((({sql}) EXCEPT ALL ({rewritten})) UNION ALL (({rewritten}) EXCEPT ALL ({sql}))) differences", connection);
                    Assert.Equal(0L, (long)(await compare.ExecuteScalarAsync())!);
                }
            }
            string nullableNotIn = $"SELECT c.id FROM {schema}.customers c WHERE c.id NOT IN (SELECT o.customer_id FROM {schema}.nullable_orders o)";
            Assert.Empty(rules.Suggest(nullableNotIn)); // Empty today is not a substitute for a NOT NULL constraint.
            await Execute($"ALTER TABLE {schema}.orders ALTER COLUMN customer_id DROP NOT NULL");
            Assert.Empty(new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault(await Metadata("customers", "orders"))).Suggest(cases[0]));
            if (connection.PostgreSqlVersion.Major >= 18)
            {
                await Execute($"ALTER TABLE {schema}.orders ADD CONSTRAINT pending_not_null NOT NULL customer_id NOT VALID");
                var pending = await Metadata("customers", "orders");
                Assert.Null(pending[1].Columns.Single(c => c.Name == "customer_id").IsNullable);
                Assert.Empty(new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault(pending)).Suggest(cases[0]));
                var column = await new PostgreSqlMetadataService().GetColumnAsync(options, "", new(schema, "orders", "customer_id"));
                Assert.Null(column!.IsNullable);
            }
            await Execute($"CREATE TABLE {schema}.child() INHERITS ({schema}.customers)");
            var inherited = await Metadata("customers"); Assert.True(inherited[0].HasDescendants);
            Assert.Empty(new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault(inherited)).Suggest(cases[2]));
            await Execute($"ALTER TABLE {schema}.composite ENABLE ROW LEVEL SECURITY");
            var rls = await Metadata("composite"); Assert.True(rls[0].RowSecurity);
            Assert.Empty(new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault(rls)).Suggest(cases[5]));
        }
        finally { await Execute($"DROP SCHEMA IF EXISTS {schema} CASCADE"); }
    }
}
