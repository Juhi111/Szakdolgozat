using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using SqlPlanVisualizer.Desktop.Configuration;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    /// <summary>
    /// A főablak adatait és parancsait kezeli:
    /// betölti az SQL-szöveget és a végrehajtási terveket,
    /// elindítja az SQL elemzését, és megjeleníti az eredményeket.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region fields  

        private readonly UserSettingsService _userSettingsService = new();
        private readonly AnalysisHistoryMapper _historyMapper = new();
        private readonly AnalysisHistoryService _historyService = new();

        private string? _analyzedSqlText;
        private string? _loadedExplainJson;

        private readonly AnalysisSummaryWindowService _summaryWindowService = new();
        private readonly CombinedAnalysisService _combinedAnalysisService = AnalysisServiceFactory.CreateCombinedAnalysisService();
        private SqlStatementSummary? _selectedSummaryStatement;
        private ExecutionPlan? _selectedSummaryPlan;
        private StatementPlanPair? _summaryPair;

        /// <summary>
        /// Az SQL-dokumentum verziója, amellyel kiszűrhetők
        /// a fájlcsere vagy felületürítés előtt indított elemzések eredményei.
        /// </summary>
        private int _sqlDocumentVersion;

        /// <summary>
        /// Az SQL-szövegből előállítja az utasítások elemzéshez szükséges összefoglalóit.
        /// </summary>
        private readonly PostgreSqlQueryService _queryService = new();        

        /// <summary>
        /// A megjelenített végrehajtási tervek gyökércsomópontjait tárolja.
        /// </summary>
        private List<PlanNode> _planRoots = new();

        // A megjelenített SQL-szöveg és állapotüzenet háttérmezői.
        private string sqlText = string.Empty;
        private string statusMessage = "Nincs betöltött fájl";       

        /// <summary>
        /// Az SQL- és JSON-fájlok kiválasztására szolgáló ablakokat nyitja meg.
        /// </summary>
        private readonly FileDialogService _fileDialogService = new();

        /// <summary>
        /// Az EXPLAIN JSON tartalmából előállítja a megjeleníthető tervmodelleket.
        /// </summary>
        private readonly PostgreSqlPlanService _planService = new();

        /// <summary>
        /// Aszinkron módon beolvassa a kiválasztott fájl szövegét.
        /// </summary>
        private readonly TextFileReader _textFileReader = new();

        /// <summary>
        /// A végrehajtási terv fájában kijelölt csomópontot tárolja.
        /// </summary>
        private PlanNode? _selectedNode;
        private readonly SqlAnalysisService _analysisService;
        private readonly PlanAnalysisService _planAnalysisService;

        #endregion

        #region properties and commands
        // A felület parancsai: SQL-elemzés, SQL- és JSON-betöltés, valamint felületürítés.
        public ICommand AnalyzeCommand { get; }
        public ICommand OpenSqlCommand { get; }
        public ICommand OpenJsonCommand { get; }
        public ICommand DeleteContent { get; }
        public ICommand CreateSummaryCommand { get; }      

        /// <summary>
        /// Az Üzenetek fülön megjelenített, időbélyeggel ellátott naplóbejegyzések.
        /// </summary>
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Az összegzéshez kiválasztható, feldolgozott SELECT utasítások.
        /// </summary>
        public ObservableCollection<SqlStatementSummary> StatementsForSummary { get; } = new();

        /// <summary>
        /// Az összegzéshez kiválasztható végrehajtási tervek.
        /// </summary>
        public ObservableCollection<ExecutionPlan> PlansForSummary { get; } = new();

        /// <summary>
        /// A Javaslatok fülön megjelenített elemzési találatok.
        /// </summary>
        public ObservableCollection<SqlAnalysisFinding> Suggestions { get; } = new();

        /// <summary>
        /// A végrehajtási tervek elemzéséből származó találatok.
        /// </summary>
        public ObservableCollection<PlanAnalysisFinding> PlanSuggestions { get; } = new();

        /// <summary>
        /// A kijelölt tervcsomópont, amelynek adatai a részletező panelen jelennek meg.
        /// Változásáról értesíti a felületet.
        /// </summary>
        public PlanNode? SelectedNode {
            get { return _selectedNode;}
            set {
                if (value == _selectedNode)
                    return;
                _selectedNode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A betöltött SQL-szöveg. Változásáról értesíti a felületet.
        /// </summary>
        public string SqlText
        {
            get => sqlText;
            set
            {
                string normalizedText = (value ?? string.Empty).ReplaceLineEndings("\r\n");

                if (normalizedText == sqlText)
                    return;

                sqlText = normalizedText;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Az állapotsor aktuális üzenete. Változásáról értesíti a felületet.
        /// </summary>
        public string StatusMessage
        {
            get { return statusMessage; }
            set {
                if (value == statusMessage)
                    return;
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A tervfában megjelenített gyökércsomópontok.
        /// A lista cseréjéről értesíti a felületet.
        /// </summary>
        public List<PlanNode> PlanRoots
        {
            get { return _planRoots; }
            set
            {
                if (value == _planRoots)
                    return;
                _planRoots = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region constructors
        /// <summary>
        /// Létrehozza a főablak parancsait, és hozzárendeli a végrehajtandó műveleteket.
        /// </summary>
        /// <summary>
        /// Létrehozza az elemző szolgáltatásokat és a főablak parancsait.
        /// </summary>
        public MainViewModel(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _analysisService = AnalysisServiceFactory.CreateSqlAnalysisService(settings);
            _planAnalysisService = AnalysisServiceFactory.CreatePlanAnalysisService(settings);
            

            OpenSqlCommand = new AsyncRelayCommand(OpenSqlAsync);
            OpenJsonCommand = new AsyncRelayCommand(OpenJsonAsync);
            AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync);
            DeleteContent = new AsyncRelayCommand(DeleteWindow);
            CreateSummaryCommand = new AsyncRelayCommand(CreateSummaryAsync);

        }
        #endregion

        #region methods

        /// <summary>
        /// Az összegzéshez kiválasztott SQL-utasítás.
        /// </summary>
        public SqlStatementSummary? SelectedSummaryStatement
        {
            get => _selectedSummaryStatement;
            set
            {
                if (_selectedSummaryStatement == value)
                    return;

                _selectedSummaryStatement = value;
                OnPropertyChanged();
                ClearSummaryPair();
            }
        }

        /// <summary>
        /// Az összegzéshez kiválasztott végrehajtási terv.
        /// </summary>
        public ExecutionPlan? SelectedSummaryPlan
        {
            get => _selectedSummaryPlan;
            set
            {
                if (_selectedSummaryPlan == value)
                    return;

                _selectedSummaryPlan = value;
                OnPropertyChanged();
                ClearSummaryPair();
            }
        }

        /// <summary>
        /// A megerősített párosítás állapotát mutatja.
        /// </summary>
        public string SummaryStatus => _summaryPair is null
            ? "Válassz SQL-utasítást és tervet, majd erősítsd meg a párosítást."
            : $"Párosítva: {_summaryPair.Statement.StatementIndex}. SQL-utasítás és {_summaryPair.Plan.PlanIndex}. terv.";

        private void ClearSummaryPair()
        {
            _summaryPair = null;            
            OnPropertyChanged(nameof(SummaryStatus));
        }

        /// <summary>
        /// Ellenőrzi a kiválasztást, és eltárolja az összetartozónak jelölt elemeket.
        /// </summary>
        private async Task CreateSummaryAsync()
        {
            if (SelectedSummaryStatement is null || SelectedSummaryPlan is null)
            {
                ReportMessage("Az összegzéshez válassz SQL-utasítást és végrehajtási tervet.");
                return;
            }

            if (!StatementsForSummary.Contains(SelectedSummaryStatement) ||
                !PlansForSummary.Contains(SelectedSummaryPlan))
            {
                ClearSummaryPair();
                ReportMessage("A kiválasztott elemek már nem tartoznak az aktuális elemzéshez.");
                return;
            }

            _summaryPair = new StatementPlanPair(SelectedSummaryStatement, SelectedSummaryPlan);

            var result = _combinedAnalysisService.Analyze(
                _summaryPair, Suggestions, PlanSuggestions);

            string? sqlSnapshot = _analyzedSqlText;
            string? jsonSnapshot = _loadedExplainJson;

            OnPropertyChanged(nameof(SummaryStatus));

            _summaryWindowService.Show(result);

            await SaveHistoryAsync(result, sqlSnapshot, jsonSnapshot);
        }

        /// <summary>
        /// Kiüríti a megjelenített SQL-t, terveket, javaslatokat és üzeneteket.
        /// A dokumentumverzió növelésével érvényteleníti a korábban indított elemzések eredményét.
        /// </summary>
        /// <returns>Egy már befejezett feladatot a parancs által elvárt visszatérési típushoz.</returns>
        private Task DeleteWindow()
        {
            _sqlDocumentVersion++;
            Suggestions.Clear();
            Messages.Clear();
            SelectedNode = null;
            StatusMessage = "Nincs betöltött fájl";
            PlanRoots = new List<PlanNode>();            
            SqlText = string.Empty;
            StatementsForSummary.Clear();
            SelectedSummaryStatement = null;
            SelectedSummaryPlan = null;
            ClearSummaryPair();
            PlansForSummary.Clear();
            PlanSuggestions.Clear();
            _analyzedSqlText = null;
            _loadedExplainJson = null;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Háttérszálon feldolgozza az aktuális SQL-szöveget, majd alkalmazza az elemzési szabályokat.
        /// Megjeleníti a találatokat és jelzi a kihagyott utasításokat vagy a feldolgozási hibát.
        /// Ha közben megváltozott a dokumentumverzió, az eredményt figyelmen kívül hagyja.
        /// </summary>
        /// <returns>Az elemzés befejezését jelző feladat.</returns>
        private async Task AnalyzeAsync()
        {
            _analyzedSqlText = null;
            Suggestions.Clear();
            StatementsForSummary.Clear();
            SelectedSummaryStatement = null;
            ClearSummaryPair();
            StatusMessage = "SQL feldolgozása…";

            string sql = SqlText;
            int documentVersion = _sqlDocumentVersion;

            var summary = await Task.Run(() => _queryService.ParseSummary(sql));

            if (documentVersion != _sqlDocumentVersion)
                return;

            if (summary.IsSuccess)
            {
                _analyzedSqlText = sql;
                foreach (var statement in summary.Statements)
                {
                    if (statement.StatementType == "SELECT")
                        StatementsForSummary.Add(statement);
                }

                SelectedSummaryStatement = StatementsForSummary.Count == 1 ? StatementsForSummary[0] : null;


                ReportMessage($"SQL feldolgozva. Utasítások száma: {summary.StatementCount}.");
                var findings = _analysisService.Analyze(summary.Statements);

                foreach (var finding in findings)
                {
                    Suggestions.Add(finding);
                }

                int skippedCount = 0;

                foreach (var statement in summary.Statements)
                {
                    if (statement.StatementType == "SELECT")
                        continue;

                    skippedCount++;

                    ReportMessage(
                        $"A(z) {statement.StatementIndex}. utasítás kimaradt az elemzésből " +
                        $"({statement.StatementType}). Jelenleg csak SELECT utasításokat elemzünk.");
                }
                ReportMessage(
                    $"Elemzés elkészült. Elemzett utasítások: {summary.StatementCount - skippedCount}. " +
                    $"Kihagyott utasítások: {skippedCount}.");
            }
            else
            {
                ReportMessage($"Hiba az SQL feldolgozásakor: {summary.ErrorMessage}");
            }
        }

        /// <summary>
        /// SQL-fájlt választat ki, törli a korábbi SQL-t és javaslatokat, majd beolvassa az új szöveget.
        /// A betöltés eredményét és az esetleges fájlhozzáférési hibát az állapotsorban jelzi.
        /// </summary>
        /// <returns>A fájlbetöltés befejezését jelző feladat.</returns>
        private async Task OpenSqlAsync() {
            
            var path = _fileDialogService.SelectSqlFile();
            if (path is null) 
                return;

            _sqlDocumentVersion++;
            SqlText = string.Empty;
            _analyzedSqlText = null;
            Suggestions.Clear();
            SelectedSummaryStatement = null;
            ClearSummaryPair();
            StatementsForSummary.Clear();

            try
            {
                StatusMessage = "SQL-fájl betöltése…";
                string content = await _textFileReader.ReadAsync(path);
                if (string.IsNullOrWhiteSpace(content))
                {
                    StatusMessage = "A kiválasztott SQL-fájl üres vagy csak üres karaktereket tartalmaz.";
                }
                else
                {
                    SqlText = content;
                    StatusMessage = $"Betöltve: {Path.GetFileName(path)}";
                }              

            }
            catch (UnauthorizedAccessException ex)
            {
                StatusMessage = $"Nincs jogosultság a fájl olvasásához: {ex.Message}";
            }
            catch(IOException ex)
            {
                StatusMessage = $"Nem sikerült beolvasni a fájlt: {ex.Message}";
            }

        }

        /// <summary>
        /// Beolvassa és feldolgozza a kiválasztott EXPLAIN JSON-fájlt,
        /// majd a terveket a felület modelljére alakítva frissíti a tervfát.
        /// A fájlhozzáférési és JSON-feldolgozási hibákat az állapotsorban jelzi.
        /// </summary>
        /// <returns>A tervbetöltés befejezését jelző feladat.</returns>
        private async Task OpenJsonAsync()
        {
            var path = _fileDialogService.SelectJsonFile();
            if (path is null) return;
            try
            {
                StatusMessage = "JSON-fájl betöltése…";
                string content = await _textFileReader.ReadAsync(path);
                if (string.IsNullOrWhiteSpace(content))
                {
                    StatusMessage = "A kiválasztott JSON-fájl üres vagy csak üres karaktereket tartalmaz.";
                }
                else
                {
                    var plans = _planService.ParsePlans(content);
                    _loadedExplainJson = content;

                    SelectedSummaryPlan = null;
                    ClearSummaryPair();
                    PlansForSummary.Clear();

                    foreach (var plan in plans)
                    {
                        PlansForSummary.Add(plan);
                    }

                    SelectedSummaryPlan = PlansForSummary.Count == 1 ? PlansForSummary[0] : null;

                    SelectedNode = null;
                    PlanRoots = plans.Select(plan => plan.Root).ToList();

                    StatusMessage = $"Betöltve: {Path.GetFileName(path)} ({plans.Count} terv)";

                    AnalyzePlans();
                    
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusMessage = $"Nincs jogosultság a fájl olvasásához: {ex.Message}";
            }
            catch (IOException ex)
            {
                StatusMessage = $"Nem sikerült beolvasni a fájlt: {ex.Message}";
            }
            catch (JsonException ex)
            {
                StatusMessage = $"Nem sikerült feldolgozni az EXPLAIN JSON-t: {ex.Message}";
            }
        }

        /// <summary>
        /// Frissíti az állapotsort, és időbélyeggel hozzáadja az üzenetet a naplóhoz.
        /// </summary>
        /// <param name="message">A megjelenítendő és naplózandó üzenet.</param>
        private void ReportMessage(string message) { 
            StatusMessage = message;
            Messages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        /// <summary>
        /// Elemzi a betöltött terveket, és frissíti a tervhez tartozó javaslatokat.
        /// </summary>
        private void AnalyzePlans()
        {
            PlanSuggestions.Clear();

            var findings = _planAnalysisService.Analyze(PlanRoots);

            foreach (var finding in findings)
            {
                PlanSuggestions.Add(finding);
            }

            ReportMessage($"Tervelemzés elkészült. Találatok száma: {findings.Count}.");
        }

        private async Task SaveHistoryAsync(CombinedAnalysisResult result, string? sqlText, string? explainJson)
        {
            try
            {
                var settings = await _userSettingsService.LoadAsync();

                if (!settings.SaveAnalysisHistory)
                    return;

                var entry = _historyMapper.Map(result, settings.IncludeSourceFiles, sqlText, explainJson);

                await _historyService.SaveAsync(entry, settings.HistoryDirectory);

                ReportMessage("Az elemzési összegzés bekerült az előzményekbe.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
            {
                ReportMessage($"Az összegzés elkészült, de az előzmény mentése nem sikerült: {ex.Message}");
            }
        }
    }
}

#endregion
