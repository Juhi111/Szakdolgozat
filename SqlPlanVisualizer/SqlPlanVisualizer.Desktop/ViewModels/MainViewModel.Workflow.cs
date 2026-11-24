using System.Windows.Input;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Infrastructure.PostgreSql;

namespace SqlPlanVisualizer.Desktop.ViewModels;

public partial class MainViewModel
{
    private readonly Dictionary<ExecutionPlan, CombinedAnalysisResult> _preparedResults = new();
    private CancellationTokenSource? _workflowCancellation;
    private bool _isAnalyzing;
    private int _analysisMode;
    public int AnalysisMode { get => _analysisMode; set { if (_isAnalyzing) return; _analysisMode = value; OnPropertyChanged(); } }
    public bool IsAnalyzing { get => _isAnalyzing; private set { _isAnalyzing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanChooseAnalysisMode)); OnPropertyChanged(nameof(CanFetchExplain)); } }
    public bool CanChooseAnalysisMode => !IsAnalyzing && !IsFetchingExplain;
    public ICommand CancelAnalysisCommand => new AsyncRelayCommand(() => { _workflowCancellation?.Cancel(); return Task.CompletedTask; });

    private async Task AnalyzeAsync()
    {
        if (IsAnalyzing || IsFetchingExplain) return;
        IsAnalyzing = true;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        _workflowCancellation = cancellation;
        _preparedResults.Clear();
        try
        {
            if (AnalysisMode is 1 or 2 && !await FetchPlanAsync(AnalysisMode == 2, true)) return;
            cancellation.Token.ThrowIfCancellationRequested();
            await AnalyzeLoadedAsync();
            if (_analyzedSqlText != SqlText) return;
            int sqlVersion = _sqlDocumentVersion, planVersion = _planDocumentVersion;
            foreach (var plan in PlansForSummary.ToArray())
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var statement = StatementsForSummary.FirstOrDefault(s => MatchesCurrentSource(s, plan));
                if (statement is null) continue; // A fájlból betöltött terv adatbázisa nem igazolt.
                var result = _combinedAnalysisService.Analyze(new(statement, plan), Suggestions, PlanSuggestions);
                if (_databaseSession.HasConfiguration)
                {
                    StatusMessage = "Az adatbázis adataival pontosítjuk a javaslatokat…";
                    var connection = _databaseSession.GetConnectionDetails();
                    try
                    {
                        var report = await new IndexMetadataAnalysisService(new PostgreSqlIndexMetadataService()).LoadAsync(result, connection.Options, connection.Password, cancellation.Token);
                        result.SetDatabaseMetadata(report);
                    }
                    catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException)
                    {
                        result.SetDatabaseMetadata(new(DateTimeOffset.Now, "Sikertelen metaadat-lekérés", [],
                            ["Az adatbázis kiegészítő adatait nem sikerült lekérni. Az SQL és a terv elemzése elkészült; az adatbázisfüggő feltételek ellenőrizetlenek maradtak."]));
                    }
                }
                if (sqlVersion != _sqlDocumentVersion || planVersion != _planDocumentVersion) return;
                _preparedResults[plan] = result;
            }
            cancellation.Token.ThrowIfCancellationRequested();
            if (sqlVersion != _sqlDocumentVersion || planVersion != _planDocumentVersion) return;
            if (StatementsForSummary.Count == 1 && PlansForSummary.Count == 1 && MatchesCurrentSource(StatementsForSummary[0], PlansForSummary[0]))
            {
                SelectedSummaryStatement = StatementsForSummary[0]; SelectedSummaryPlan = PlansForSummary[0];
                await CreateSummaryAsync(true);
            }
            else if (PlansForSummary.Count == 0) ReportMessage("Az SQL elemzése elkészült. A javaslatokat a Javaslatok fülön találod; tervhez válassz adatbázisos módot vagy tölts be EXPLAIN fájlt.");
            else ReportMessage("Az elemzés elkészült. Az Összegzés fülön válaszd ki az összetartozó SQL-t és tervet.");
        }
        catch (OperationCanceledException) { ReportMessage("Az elemzést megszakítottuk vagy lejárt az időkerete. Új összegzés nem készült."); }
        catch (Exception) { ReportMessage("Az elemzés nem fejeződött be. Ellenőrizd a bemenetet és a kapcsolatot, majd próbáld újra."); }
        finally { _workflowCancellation = null; IsAnalyzing = false; }
    }

    private IDatabaseMetadataService CreateMetadataReader(CombinedAnalysisResult result)
    {
        if (result.DatabaseMetadata is null) return new PostgreSqlMetadataService();
        var origin = result.Pair.Plan.Origin!;
        var source = new DatabaseConnectionOptions { Host = origin.Host!, Port = origin.Port!.Value, Database = origin.Database!, Schema = origin.Schema!, Username = origin.Username! };
        var data = result.DatabaseMetadata.Tables.ToDictionary(t => new DatabaseTableReference(t.Metadata.Schema, t.Metadata.Table), t => (DatabaseTableMetadata?)t.Metadata);
        foreach (var group in result.DatabaseMetadata.Tables.GroupBy(t => t.Metadata.Table))
        {
            var resolved = group.FirstOrDefault(t => t.Metadata.Schema == "pg_catalog") ?? group.FirstOrDefault(t => t.Metadata.Schema == source.Schema);
            if (resolved is not null) data[new(null, group.Key)] = resolved.Metadata;
        }
        return new AnalysisMetadataSnapshotReader(source, data, capturedAt: result.DatabaseMetadata.CapturedAt);
    }
}
