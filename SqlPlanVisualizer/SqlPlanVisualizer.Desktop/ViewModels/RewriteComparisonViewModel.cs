using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Infrastructure.PostgreSql;

namespace SqlPlanVisualizer.Desktop.ViewModels;

/// <summary>Egy összegző ablak javaslatainak tervlekérését és összehasonlítását kezeli.</summary>
public sealed class RewriteComparisonViewModel : ViewModelBase
{
    private readonly CombinedAnalysisResult _original;
    private readonly DatabaseSession _session;
    private readonly SqlRewriteComparisonService _service;
    private readonly Func<bool> _confirm;
    private CancellationTokenSource? _cancellation;
    private SqlRewriteSuggestion? _selectedSuggestion;
    private bool _isBusy;
    private string _status = "Válassz javaslatot. Az eredeti terv megmarad; a lekért javaslatot külön elemezzük.";
    private SqlRewriteComparison? _comparison;

    public RewriteComparisonViewModel(CombinedAnalysisResult original, IReadOnlyList<SqlRewriteSuggestion> suggestions,
        DatabaseSession session, SqlRewriteComparisonService service, Func<bool>? confirm = null)
    {
        _original = original;
        Suggestions = suggestions;
        _session = session;
        _service = service;
        _confirm = confirm ?? (() => MessageBox.Show("A javasolt SQL ténylegesen lefut az adatbázison EXPLAIN ANALYZE módban. " +
            "Ellenőrizted az átírás alkalmazási feltételeit, és elindítod a mérést?", "Javaslat futtatása",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
        _selectedSuggestion = suggestions.FirstOrDefault();
        MetadataCheck = ReadSnapshotCheck(_selectedSuggestion);
        CheckMetadataCommand = new AsyncRelayCommand(CheckMetadataAsync);
        EstimatedCommand = new AsyncRelayCommand(() => FetchAsync(false));
        ActualCommand = new AsyncRelayCommand(() => FetchAsync(true));
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        CancelCommand = new AsyncRelayCommand(() => { Cancel(); return Task.CompletedTask; });
    }

    private RewriteMetadataCheck _metadataCheck = RewriteMetadataCheck.NotChecked();
    public RewriteMetadataCheck MetadataCheck
    {
        get => _metadataCheck;
        private set { _metadataCheck = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStart)); }
    }
    public ICommand CheckMetadataCommand { get; }
    public async Task CheckMetadataAsync()
    {
        if (IsBusy || SelectedSuggestion is null) return;
        MetadataCheck = RewriteMetadataCheck.NotChecked();
        if (!_session.HasConfiguration) { Status = "Metaadat-ellenőrzéshez válassz adatbázis-kapcsolatot."; return; }
        IsBusy = true;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        try
        {
            var connection = _session.GetConnectionDetails();
            Status = "Az oszlop metaadatainak lekérése…";
            var check = await _service.CheckMetadataAsync(_original, SelectedSuggestion, connection.Options, connection.Password, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            MetadataCheck = check;
            Status = string.Empty;
        }
        catch (OperationCanceledException) { Status = "A metaadat-ellenőrzést megszakítottuk."; }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { Status = ex.Message; }
        finally { _cancellation = null; IsBusy = false; }
    }

    public IReadOnlyList<SqlRewriteSuggestion> Suggestions { get; }
    public ICommand EstimatedCommand { get; }
    public ICommand ActualCommand { get; }
    public ICommand ExportCommand { get; }
    public bool CanExport => Comparison is not null && !IsBusy;
    private Task ExportAsync()
    {
        var snapshot = Comparison;
        return CanExport && snapshot is not null ? new PdfExportService().ExportComparisonToPdfAsync(snapshot) : Task.CompletedTask;
    }
    public ICommand CancelCommand { get; }
    public ObservableCollection<ComparisonMetric> Metrics { get; } = new();
    public SqlRewriteComparison? Comparison { get => _comparison; private set { _comparison = value; OnPropertyChanged(); OnPropertyChanged(nameof(OriginalRoots)); OnPropertyChanged(nameof(SuggestedRoots)); OnPropertyChanged(nameof(CanExport)); } }
    public IEnumerable<PlanNode> OriginalRoots => Comparison is null ? Array.Empty<PlanNode>() : new[] { Comparison.Original.Pair.Plan.Root };
    public IEnumerable<PlanNode> SuggestedRoots => Comparison is null ? Array.Empty<PlanNode>() : new[] { Comparison.SuggestedPlan.Root };
    public bool IsBusy { get => _isBusy; private set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStart)); OnPropertyChanged(nameof(CanSelect)); OnPropertyChanged(nameof(CanExport)); } }
    public bool CanSelect => !IsBusy;
    public bool CanStart => !IsBusy && SelectedSuggestion is not null && !MetadataCheck.BlocksExecution;
    public string Status { get => _status; private set { _status = value; OnPropertyChanged(); } }
    public SqlRewriteSuggestion? SelectedSuggestion
    {
        get => _selectedSuggestion;
        set
        {
            if (IsBusy || ReferenceEquals(value, _selectedSuggestion)) return;
            _selectedSuggestion = value;
            MetadataCheck = ReadSnapshotCheck(value);
            Comparison = null;
            Metrics.Clear();
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStart));
        }
    }
    private RewriteMetadataCheck ReadSnapshotCheck(SqlRewriteSuggestion? suggestion)
    {
        if (suggestion?.RequiresRelationalMetadata == true)
            return RewriteMetadataCheck.NotChecked("A javaslat a lekért kulcs- és NULL-adatokra épül. Futtatás előtt friss adatbázisadatokkal újra ellenőrizzük.");
        if (suggestion?.MetadataRequirement is not { } requirement || _original.DatabaseMetadata is not { } report)
            return RewriteMetadataCheck.NotChecked("A szükséges feltételt a terv lekérése előtt automatikusan ellenőrizzük.");
        var sourceSchema = _original.Pair.Plan.Origin?.Schema;
        var candidates = report.Tables.Select(t => t.Metadata).Where(t => t.Table == requirement.Column.Table);
        var table = requirement.Column.Schema is string schema ? candidates.FirstOrDefault(t => t.Schema == schema)
            : candidates.FirstOrDefault(t => t.Schema == "pg_catalog") ?? candidates.FirstOrDefault(t => t.Schema == sourceSchema);
        var item = table?.Columns.FirstOrDefault(c => c.Name == requirement.Column.Column);
        var column = item is null || table is null ? null : new DatabaseColumnMetadata(table.Schema, table.Table, item.Name, item.TypeSchema, item.TypeName, item.DisplayType, item.IsDomain, item.IsNullable);
        return SqlPlanVisualizer.Core.Analysis.RewriteMetadataEvaluator.Evaluate(requirement.Type, column) with { CheckedAt = report.CapturedAt, Source = report.Source };
    }

    public void Cancel() => _cancellation?.Cancel();

    public async Task FetchAsync(bool analyze)
    {
        if (!CanStart) return;
        if (!_session.HasConfiguration) { Status = "Először válassz adatbázis-kapcsolatot."; return; }
        var suggestion = SelectedSuggestion!;
        IsBusy = true;
        try
        {
            var connection = _session.GetConnectionDetails();
            SqlRewriteComparisonService.ValidateSource(_original, suggestion, connection.Options);
            if (analyze && !_confirm()) { Status = "A mérést nem indítottuk el."; return; }
            using var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            Comparison = null;
            Metrics.Clear();
            Status = analyze ? "A javaslat futtatása és mérése…" : "A javaslat becsült tervének lekérése…";
            var result = await _service.CompareAsync(_original, suggestion, connection.Options, connection.Password, analyze, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            MetadataCheck = result.MetadataCheck;
            Comparison = result;
            AddMetrics(result);
            Status = "Az összehasonlítás elkészült.";
        }
        catch (OperationCanceledException) { Status = "A tervlekérést megszakítottuk."; }
        catch (RewriteMetadataException ex) { MetadataCheck = ex.Check; Status = string.Empty; }
        catch (InvalidOperationException ex) { Status = ex.Message; }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or JsonException or ArgumentException)
        {
            Status = "Nem sikerült lekérni vagy feldolgozni a javaslat tervét. Ellenőrizd a kapcsolatot, a jogosultságot és az SQL alkalmazhatóságát.";
        }
        finally { _cancellation = null; IsBusy = false; }
    }

    private void AddMetrics(SqlRewriteComparison result)
    {
        var before = result.Original.Pair.Plan;
        var after = result.SuggestedPlan;
        Metrics.Add(new("Terv módja", Mode(before.Origin?.Mode), Mode(after.Origin?.Mode)));
        Metrics.Add(new("Tervezési idő (ms)", Number(before.PlanningTime), Number(after.PlanningTime)));
        Metrics.Add(new("Végrehajtási idő (ms)", Number(before.ExecutionTime), Number(after.ExecutionTime)));
        Metrics.Add(new("Gyökér becsült teljes költsége", Number(before.Root.TotalCost), Number(after.Root.TotalCost)));
        Metrics.Add(new("Gyökér tényleges sorai / végrehajtás", Number(before.Root.ActualRows), Number(after.Root.ActualRows)));
        Metrics.Add(new("SQL-találatok száma", result.Original.SqlFindings.Count.ToString(), result.SqlFindings.Count.ToString()));
        Metrics.Add(new("Tervtalálatok száma", result.Original.PlanFindings.Count.ToString(), result.PlanFindings.Count.ToString()));
    }
    private static string Mode(PlanCaptureMode? mode) => mode switch { PlanCaptureMode.Actual => "Mért", PlanCaptureMode.Estimated => "Becsült", _ => "Ismeretlen" };
    private static string Number(double? value) => value is double number && double.IsFinite(number) ? number.ToString("N3") : "Nincs adat";
}

public sealed record ComparisonMetric(string Name, string Original, string Suggested);
