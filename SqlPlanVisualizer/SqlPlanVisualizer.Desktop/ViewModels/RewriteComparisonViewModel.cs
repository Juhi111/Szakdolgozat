using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Npgsql;
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
        EstimatedCommand = new AsyncRelayCommand(() => FetchAsync(false));
        ActualCommand = new AsyncRelayCommand(() => FetchAsync(true));
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        CancelCommand = new AsyncRelayCommand(() => { Cancel(); return Task.CompletedTask; });
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
    public bool CanStart => !IsBusy && SelectedSuggestion is not null;
    public string Status { get => _status; private set { _status = value; OnPropertyChanged(); } }
    public SqlRewriteSuggestion? SelectedSuggestion
    {
        get => _selectedSuggestion;
        set
        {
            if (IsBusy || ReferenceEquals(value, _selectedSuggestion)) return;
            _selectedSuggestion = value;
            Comparison = null;
            Metrics.Clear();
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStart));
        }
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
            Comparison = result;
            AddMetrics(result);
            Status = "A javaslat tervét lekértük és külön elemeztük. Az eredeti elemzés változatlan.";
        }
        catch (OperationCanceledException) { Status = "A tervlekérést megszakítottuk."; }
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
