using System.Text.Json.Serialization;

namespace SqlPlanVisualizer.Core.Models;

public enum PlanSourceKind { Unknown, JsonFile, Database }
public enum PlanCaptureMode { Unknown, Estimated, Actual }

/// <summary>Változatlan pillanatkép a terv forrásáról. Nem tartalmaz jelszót vagy connection stringet.</summary>
public sealed record PlanOrigin
{
    public PlanSourceKind Source { get; init; }
    public PlanCaptureMode Mode { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public string? FileName { get; init; }
    public string? SqlText { get; init; }
    public string? DocumentHash { get; init; }
    public int? StatementIndex { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? Database { get; init; }
    public string? Schema { get; init; }
    public string? Username { get; init; }

    [JsonIgnore]
    public string Description => Source switch
    {
        PlanSourceKind.Database => $"Adatbázisból lekérve: {Host}:{Port} / {Database}; séma: {Schema}; felhasználó: {Username}. " +
            $"{(Mode == PlanCaptureMode.Actual ? "Mért" : "Becsült")} terv; {CapturedAt.ToLocalTime():yyyy.MM.dd. HH:mm:ss zzz}; SQL-utasítás: {StatementIndex}.",
        PlanSourceKind.JsonFile => $"JSON-fájlból betöltve: {FileName}. Betöltés: {CapturedAt.ToLocalTime():yyyy.MM.dd. HH:mm:ss zzz}. A terv eredeti készítési ideje és kapcsolata nem ismert.",
        _ => "A terv eredete nem ismert."
    };
}
