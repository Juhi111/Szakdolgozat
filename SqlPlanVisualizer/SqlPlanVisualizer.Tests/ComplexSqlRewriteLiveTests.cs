using Npgsql;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class ComplexSqlRewriteLiveTests
{
    [PostgreSqlMetadataFact]
    public async Task CompleteQueriesAndDateBoundaries_PreserveRowsIncludingNullsAndDuplicates()
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SQLPLAN_METADATA_TEST_CONNECTION")!);
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        string schema = "complex_" + Guid.NewGuid().ToString("N");
        var options = new DatabaseConnectionOptions { Host = builder.Host!, Port = builder.Port, Database = builder.Database!, Username = builder.Username!, Schema = schema, TlsMode = DatabaseTlsMode.Disable };
        async Task Execute(string sql) { await using var command = new NpgsqlCommand(sql, connection); await command.ExecuteNonQueryAsync(); }
        async Task Equal(string original, string changed)
        {
            await using var command = new NpgsqlCommand($"SELECT count(*) FROM ((({original}) EXCEPT ALL ({changed})) UNION ALL (({changed}) EXCEPT ALL ({original}))) differences", connection);
            Assert.True((long)(await command.ExecuteScalarAsync())! == 0, $"Different results:\n{original}\n{changed}");
        }
        try
        {
            await Execute($"""
                CREATE SCHEMA {schema};
                CREATE TABLE {schema}.customers(customer_id bigint PRIMARY KEY, first_name varchar, last_name varchar, email varchar, city varchar, registered_at timestamptz, marketing_opt_in bool, country_code char(2));
                CREATE TABLE {schema}.orders(order_id bigint PRIMARY KEY, customer_id bigint, order_date timestamptz, total_amount numeric, sales_channel varchar, shipping_fee numeric);
                CREATE TABLE {schema}.order_items(order_id bigint, product_id bigint, quantity int);
                CREATE TABLE {schema}.products(product_id bigint PRIMARY KEY, product_name varchar);
                CREATE TABLE {schema}.reviews(customer_id bigint, rating smallint);
                CREATE TABLE {schema}.dates(d date, t timestamp, z timestamptz, flag bool, amount numeric);
                INSERT INTO {schema}.customers VALUES
                  (101,'Adam','Kovacs','adam@gmail.com','Budapest','2023-01-01 00:00+01',true,'HU'),
                  (102,'Anna','Kiss','anna@gmail.com','Budapest','2024-02-29 12:00+01',true,'HU'),
                  (103,NULL,'Null','null@gmail.com','Budapest',NULL,NULL,'HU'),
                  (104,'Other','Test','other@example.com','London','2027-01-01 00:00+01',false,'GB');
                INSERT INTO {schema}.orders VALUES (1,101,'2024-01-01',500.25,'web',10),(2,101,'2024-01-02',501.25,'web',20),(3,102,'2024-02-29',800.75,'app',NULL),(4,104,'2022-12-31',NULL,'web',10);
                INSERT INTO {schema}.order_items VALUES(1,1,2),(1,1,2),(2,1,1),(3,1,1);
                INSERT INTO {schema}.products VALUES(1,'Alma');
                INSERT INTO {schema}.reviews VALUES(102,5),(102,4),(104,NULL);
                INSERT INTO {schema}.dates VALUES
                  (NULL,NULL,NULL,NULL,NULL),
                  ('-infinity','-infinity','-infinity',false,'-Infinity'),
                  ('infinity','infinity','infinity',true,'Infinity');
                INSERT INTO {schema}.dates
                SELECT x::date,x::timestamp,x::timestamptz,true,150.25 FROM unnest(ARRAY[
                  '2022-12-31 23:59:59+00','2023-01-01 00:00:00+00','2023-01-01 00:59:59+00',
                  '2023-12-31 23:59:59+00','2024-01-01 00:00:00+00','2024-02-29 00:00:00+00',
                  '2024-02-29 23:59:59+00','2024-03-01 00:00:00+00','2026-12-31 23:59:59+00','2027-01-01 00:00:00+00']) x;
                """);
            var reference = new DatabaseTableReference(schema, "customers");
            var metadata = await new PostgreSqlIndexMetadataService().LoadSnapshotAsync(options, builder.Password ?? "", [reference]);
            var table = (await metadata.GetTableAsync(options, "", reference))!;
            var engine = new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault([table]));
            var examples = ComplexSqlRewriteTests.Examples();
            for (int i = 0; i < examples.Length; i++)
            {
                string sql = examples[i].Replace("szakdoga_lab", schema);
                // Exercise the grouped result as well as the uploaded OFFSET that returns an empty set here.
                if (i == 3) sql = sql.Replace("OFFSET 20000", "OFFSET 0");
                var suggestion = engine.SuggestCombined(sql)!;
                if (i == 0) { Assert.Equal(6, suggestion.AppliedSteps.Count); Assert.DoesNotContain("DISTINCT", suggestion.SuggestedSql); }
                if (i == 1)
                {
                    // random() intentionally remains; compare stable values and multiplicity, not random numbers.
                    await Equal($"SELECT customer_id,first_name,last_name,email FROM ({sql}) q", $"SELECT customer_id,first_name,last_name,email FROM ({suggestion.SuggestedSql}) q");
                }
                else await Equal(sql, suggestion.SuggestedSql);
            }
            foreach (string zone in new[] { "UTC", "Europe/Budapest", "America/New_York" })
            {
                await Execute($"SET TIME ZONE '{zone}'");
                foreach (string column in new[] { "d", "t", "z" })
                foreach (string op in new[] { "=", "<", "<=", ">", ">=" })
                foreach (string expression in new[] { $"{column}::date {op} DATE '2024-02-29'", $"DATE '2024-02-29' {op} {column}::date", $"EXTRACT(YEAR FROM {column}) {op} 2024", $"2024 {op} EXTRACT(YEAR FROM {column})" })
                {
                    string sql = $"SELECT * FROM {schema}.dates WHERE {expression}";
                    await Equal(sql, engine.SuggestCombined(sql)!.SuggestedSql);
                }
            }
            string booleanSql = $"SELECT * FROM {schema}.dates WHERE CASE WHEN flag THEN 1 ELSE 0 END = 1";
            await Equal(booleanSql, engine.SuggestCombined(booleanSql)!.SuggestedSql);
            string numericSql = $"SELECT * FROM {schema}.dates WHERE amount + 50 > 200";
            await Equal(numericSql, engine.SuggestCombined(numericSql)!.SuggestedSql);
        }
        finally { await Execute($"DROP SCHEMA IF EXISTS {schema} CASCADE"); }
    }
}
