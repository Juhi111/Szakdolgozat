using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

/// <summary>Friss katalógusadatokból újragenerálja a javaslatot; ismeretlen feltétellel nem engedi futni.</summary>
internal static class RelationalRewritePreflight
{
    public static async Task<RewriteMetadataCheck> CheckAsync(SqlRewriteSuggestion suggestion, DatabaseConnectionOptions options,
        string password, IDatabaseIndexMetadataService? service, CancellationToken token)
    {
        try
        {
            var references = RelationalRewriteRule.TableReferences(suggestion.OriginalSql);
            service ??= new PostgreSqlIndexMetadataService();
            if (service is PostgreSqlIndexMetadataService postgres)
                service = await postgres.LoadSnapshotAsync(options, password, references, token);
            var tables = new List<DatabaseTableMetadata>();
            foreach (var reference in references)
            {
                token.ThrowIfCancellationRequested();
                var table = await service.GetTableAsync(options, password, reference, token);
                if (table is null) return Failed("A szükséges táblaadatok nem érhetők el. A javaslatot nem futtatjuk; kérj új elemzést.");
                tables.Add(table);
            }
            token.ThrowIfCancellationRequested();
            bool valid = SqlRewriteRuleFactory.CreateDefault(tables).SelectMany(r => r.Suggest(suggestion.OriginalSql))
                .Any(s => s.RequiresRelationalMetadata && s.RuleId == suggestion.RuleId && s.SuggestedSql == suggestion.SuggestedSql);
            return valid
                ? new(MetadataCheckStatus.Passed, "A kulcs-, típus- és NULL-feltételeket friss adatbázisadatokkal újra ellenőriztük.", CheckedAt: DateTimeOffset.UtcNow,
                    Source: $"{options.Host}:{options.Port}/{options.Database}; felhasználó: {options.Username}")
                : Failed("A javaslat feltételei már nem igazolhatók, vagy a javasolt SQL megváltozott. Kérj új elemzést.");
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException or FormatException)
        {
            token.ThrowIfCancellationRequested();
            return Failed("A friss adatbázis-ellenőrzés nem sikerült. A javaslatot nem futtatjuk; ellenőrizd a kapcsolatot és a jogosultságokat.");
        }
    }
    private static RewriteMetadataCheck Failed(string message) => new(MetadataCheckStatus.Failed, message);
}
