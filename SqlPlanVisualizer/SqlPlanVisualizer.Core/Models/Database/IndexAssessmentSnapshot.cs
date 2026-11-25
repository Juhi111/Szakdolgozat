using System.Text.Json.Serialization;

namespace SqlPlanVisualizer.Core.Models.Database;

public sealed record IndexAssessmentItem(string Table, string Title, string Description);

/// <summary>Az adott elemzéshez lekért metaadatok megállapításainak változatlan pillanatképe.</summary>
public sealed class IndexAssessmentSnapshot
{
    public const string NotChecked = "Az adatbázis kiegészítő adatait nem ellenőriztük. Az indexekre vonatkozó általános javaslatok alkalmazása előtt kérd le a metaadatokat.";
    public DateTimeOffset CapturedAt { get; }
    public string Source { get; }
    public IReadOnlyList<IndexAssessmentItem> Items { get; }
    public IReadOnlyList<string> Notices { get; }
    public IReadOnlyList<string> Evidence { get; }
    [JsonIgnore] public string SourceText => $"{Source}; lekérve: {CapturedAt:yyyy.MM.dd. HH:mm:ss zzz}. A terv és a metaadat külön időpontban készült; az indexek azóta változhattak.";
    [JsonIgnore] public string Guidance => "Az alábbi adatbázisadatok pontosítják az általános javaslatokat. Meglévő jelölt esetén előbb annak alkalmazhatóságát vizsgáld meg. A lehetséges illeszkedés nem bizonyít indexhasználatot vagy gyorsulást.";

    [JsonConstructor]
    public IndexAssessmentSnapshot(DateTimeOffset capturedAt, string source, IReadOnlyList<IndexAssessmentItem> items, IReadOnlyList<string> notices, IReadOnlyList<string>? evidence = null)
    {
        if (source is null || items is null || notices is null || items.Any(i => i is null) || notices.Any(n => n is null))
            throw new System.Text.Json.JsonException("Hiányos indexértékelési pillanatkép.");
        Evidence = Array.AsReadOnly((evidence ?? Array.Empty<string>()).ToArray());
        CapturedAt = capturedAt; Source = source;
        Items = Array.AsReadOnly(items.ToArray()); Notices = Array.AsReadOnly(notices.ToArray());
    }

    private static string ScopeDescription(DatabaseTableMetadata table)
    {
        static string Flag(bool? value) => value is null ? "ismeretlen" : value.Value ? "igen" : "nem";
        string kind = table.RelationKind switch { "r" => "tábla", "p" => "particionált tábla", "v" => "nézet", "m" => "materializált nézet", "f" => "külső tábla", _ => "ismeretlen" };
        return $"{table.DisplayName}: típus: {kind}; vannak leszármazottak: {Flag(table.HasDescendants)}; sorbiztonság bekapcsolva: {Flag(table.RowSecurity)}";
    }

    public static IndexAssessmentSnapshot Capture(IndexMetadataReport report) => new(report.CapturedAt, report.Source,
        report.Tables.SelectMany(t => t.Observations.Select(o => new IndexAssessmentItem(t.Metadata.DisplayName, o.Title, o.Description))).ToArray(), report.Notices,
        report.Tables.SelectMany(t => t.Metadata.Indexes.Select(i => t.Metadata.DisplayName + ": " + i.Definition + " | " + i.StateText)
            .Concat(t.Metadata.Keys.Select(k => t.Metadata.DisplayName + ": " + k.Details))
            .Concat(t.Metadata.Columns.Select(c => t.Metadata.DisplayName + ": " + c.Description))
            .Concat(new[] { t.Metadata.DisplayName + ": " + t.Metadata.Statistics.Description,
                ScopeDescription(t.Metadata) })
            .Concat(t.Metadata.ColumnStatistics.Select(c => t.Metadata.DisplayName + ": " + c))
            .Concat(t.Metadata.ExtendedStatistics)
            .Concat(t.Metadata.Extensions.Select(e => "Bővítmény: " + e))
            .Concat(t.Metadata.Settings.Select(s => s.Key + ": " + s.Value))).Distinct().ToArray());
}
