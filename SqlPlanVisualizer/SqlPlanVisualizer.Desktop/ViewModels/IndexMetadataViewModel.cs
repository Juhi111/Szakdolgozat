using System.Windows.Input;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Infrastructure.PostgreSql;

namespace SqlPlanVisualizer.Desktop.ViewModels;

public sealed class IndexMetadataViewModel : ViewModelBase
{
    private readonly CombinedAnalysisResult _result;
    private readonly DatabaseSession? _session;
    private CancellationTokenSource? _cancellation;
    private IndexMetadataReport? _report;
    private string _status = "A lekérés az eredeti tervhez tartozó adatbázis aktuális indexeit és PRIMARY KEY / UNIQUE kulcsait mutatja. Nem módosítja az adatbázist.";
    public IndexMetadataViewModel(CombinedAnalysisResult result, DatabaseSession? session)
    {
        _result = result; _session = session;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }
    public ICommand LoadCommand { get; }
    public IndexMetadataReport? Report { get => _report; private set { _report = value; OnPropertyChanged(); } }
    public string StatusMessage { get => _status; private set { _status = value; OnPropertyChanged(); } }
    public void Cancel() => _cancellation?.Cancel();
    private async Task LoadAsync()
    {
        // A legutóbbi sikeres pillanatkép megmarad sikertelen frissítés esetén.
        if (_session?.HasConfiguration != true) { StatusMessage = "Válassz adatbázis-kapcsolatot, majd kérd le a tervet."; return; }
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        StatusMessage = "Indexek és kulcsok lekérése…";
        try
        {
            var details = _session.GetConnectionDetails();
            var report = await new IndexMetadataAnalysisService(new PostgreSqlIndexMetadataService()).LoadAsync(_result, details.Options, details.Password, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            _result.SetIndexAssessment(IndexAssessmentSnapshot.Capture(report));
            Report = report;
            StatusMessage = "A lekérés befejeződött. A szerkezeti egyezés nem bizonyítja az index alkalmazhatóságát.";
        }
        catch (OperationCanceledException) { StatusMessage = "A lekérést megszakítottuk."; }
        catch (InvalidOperationException ex) { StatusMessage = ex.Message; }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException or ArgumentException or System.Text.Json.JsonException)
        { StatusMessage = "A metaadatok nem tölthetők be. Ellenőrizd a kapcsolatot, a jogosultságokat és a szerver támogatását (PostgreSQL 15 vagy újabb)."; }
        finally { _cancellation = null; }
    }
}
