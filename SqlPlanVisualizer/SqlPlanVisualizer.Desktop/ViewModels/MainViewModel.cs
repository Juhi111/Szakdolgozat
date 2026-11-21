using Npgsql;
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
using SqlPlanVisualizer.Core.Interfaces;
using System.Windows;

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

        private readonly DatabaseSession _databaseSession;
        private readonly IDatabaseExplainService _databaseExplainService;
        private bool _isFetchingExplain;
        private readonly UserSettingsService _userSettingsService = new();
        private readonly AnalysisHistoryMapper _historyMapper = new();
        private readonly AnalysisHistoryService _historyService = new();

        private string? _analyzedSqlText;
        private string? _loadedExplainJson;
        private List<ExecutionPlan> _loadedPlans = new();
        private int _planDocumentVersion;

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
        public ICommand FetchEstimatedPlanCommand { get; }
        public ICommand FetchActualPlanCommand { get; }

        public bool IsFetchingExplain
        {
            get => _isFetchingExplain;
            private set
            {
                if (_isFetchingExplain == value)
                    return;
                _isFetchingExplain = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanFetchExplain));
            }
        }

        public bool CanFetchExplain => !IsFetchingExplain;


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

                _sqlDocumentVersion++;
                sqlText = normalizedText;
                OnPropertyChanged();
                ClearSummaryPair();
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
        public MainViewModel(AppSettings settings, DatabaseSession databaseSession, IDatabaseExplainService databaseExplainService)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(databaseSession);
            ArgumentNullException.ThrowIfNull(databaseExplainService);

            _databaseSession = databaseSession;
            _databaseExplainService = databaseExplainService;

            _analysisService = AnalysisServiceFactory.CreateSqlAnalysisService(settings);
            _planAnalysisService = AnalysisServiceFactory.CreatePlanAnalysisService(settings);

            OpenSqlCommand = new AsyncRelayCommand(OpenSqlAsync);
            OpenJsonCommand = new AsyncRelayCommand(OpenJsonAsync);
            AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync);
            DeleteContent = new AsyncRelayCommand(DeleteWindow);
            CreateSummaryCommand = new AsyncRelayCommand(CreateSummaryAsync);
            FetchEstimatedPlanCommand = new AsyncRelayCommand(() => FetchPlanAsync(false));
            FetchActualPlanCommand = new AsyncRelayCommand(() => FetchPlanAsync(true));
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
                TryAutoPair();
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
                OnPropertyChanged(nameof(SelectedPlanOriginText));
                ClearSummaryPair();
                SelectOriginalStatement();
            }
        }

        /// <summary>
        /// A megerősített párosítás állapotát mutatja.
        /// </summary>
        public string SelectedPlanOriginText => SelectedSummaryPlan?.Origin?.Description ?? "A terv eredete nem ismert.";
        public string PairingInstruction => SelectedSummaryPlan?.Origin?.Source == PlanSourceKind.Database
            ? "Az adatbázisból lekért tervet csak az eredeti SQL-utasításával lehet összesíteni. A párosítás automatikus."
            : "Az összegzés készítésével megerősíted, hogy a kiválasztott terv a kiválasztott SQL-utasításhoz tartozik.";

        public string SummaryStatus => _summaryPair is not null
            ? $"{(_summaryPair.Plan.Origin?.Source == PlanSourceKind.Database ? "Automatikusan párosítva" : "Párosítva")}: {_summaryPair.Statement.StatementIndex}. SQL-utasítás és {_summaryPair.Plan.PlanIndex}. terv."
            : SelectedSummaryPlan?.Origin?.Source == PlanSourceKind.Database
                ? "A tervhez tartozó eredeti SQL nincs kiválasztva vagy megváltozott. Töltsd vissza és elemezd az eredeti dokumentumot, vagy kérj új tervet."
                : "Válassz SQL-utasítást és tervet, majd erősítsd meg a párosítást.";

        private bool MatchesCurrentSource(SqlStatementSummary statement, ExecutionPlan plan) =>
            _analyzedSqlText == SqlText && StatementsForSummary.Contains(statement) && PlansForSummary.Contains(plan) &&
            PlanOriginMatcher.Matches(plan.Origin, SqlText, statement);

        private void SelectOriginalStatement()
        {
            if (SelectedSummaryPlan?.Origin?.Source == PlanSourceKind.Database)
            {
                SelectedSummaryStatement = StatementsForSummary.FirstOrDefault(statement => MatchesCurrentSource(statement, SelectedSummaryPlan));
                TryAutoPair();
            }
        }

        private void TryAutoPair()
        {
            if (SelectedSummaryStatement is { } statement && SelectedSummaryPlan is { } plan && MatchesCurrentSource(statement, plan))
                _summaryPair = new StatementPlanPair(statement, plan);
            OnPropertyChanged(nameof(SummaryStatus));
            OnPropertyChanged(nameof(PairingInstruction));
        }

        private void ClearSummaryPair()
        {
            _summaryPair = null;            
            OnPropertyChanged(nameof(SummaryStatus));
            OnPropertyChanged(nameof(PairingInstruction));
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

            if (SelectedSummaryPlan.Origin?.Source == PlanSourceKind.Database && !MatchesCurrentSource(SelectedSummaryStatement, SelectedSummaryPlan))
            {
                ClearSummaryPair();
                ReportMessage("A lekért terv nem ehhez az SQL-utasításhoz tartozik. Elemezd az eredeti SQL-t, vagy kérj új tervet.");
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
            _loadedPlans.Clear();
            _planDocumentVersion++;

            return Task.CompletedTask;
        }

        /// <summary>
        /// A betöltött SQL és tervek elemzését párhuzamosan futtatja.
        /// Csak a változatlan bemenethez tartozó eredményeket jeleníti meg.
        /// </summary>
        private async Task AnalyzeAsync()
        {
            string sql = SqlText;
            var plans = _loadedPlans.ToArray();
            int sqlVersion = _sqlDocumentVersion;
            int planVersion = _planDocumentVersion;
            bool hasSql = !string.IsNullOrWhiteSpace(sql);

            _analyzedSqlText = null;
            Suggestions.Clear();
            PlanSuggestions.Clear();
            StatementsForSummary.Clear();
            PlansForSummary.Clear();
            SelectedSummaryStatement = null;
            SelectedSummaryPlan = null;
            ClearSummaryPair();

            if (!hasSql && plans.Length == 0)
            {
                ReportMessage("Az elemzéshez tölts be SQL- vagy EXPLAIN JSON-fájlt.");
                return;
            }

            StatusMessage = "SQL és végrehajtási tervek elemzése…";

            var sqlTask = Task.Run(() =>
            {
                var summary = hasSql ? _queryService.ParseSummary(sql) : null;
                var findings = summary?.IsSuccess == true
                    ? _analysisService.Analyze(summary.Statements)
                    : new List<SqlAnalysisFinding>();
                return (Summary: summary, Findings: findings);
            });
            var planTask = Task.Run(() => _planAnalysisService.Analyze(plans.Select(plan => plan.Root)));

            try
            {
                await Task.WhenAll(sqlTask, planTask);
                var sqlResult = await sqlTask;
                var planFindings = await planTask;

                if (sqlVersion == _sqlDocumentVersion && sqlResult.Summary is { } summary)
                {
                    if (summary.IsSuccess)
                    {
                        _analyzedSqlText = sql;
                        foreach (var statement in summary.Statements)
                        {
                            if (statement.StatementType == "SELECT")
                                StatementsForSummary.Add(statement);
                            else
                                ReportMessage($"A(z) {statement.StatementIndex}. utasítás kimaradt ({statement.StatementType}). Csak SELECT utasításokat elemzünk.");
                        }

                        foreach (var finding in sqlResult.Findings)
                            Suggestions.Add(finding);

                        SelectedSummaryStatement = StatementsForSummary.Count == 1 ? StatementsForSummary[0] : null;
                        ReportMessage($"SQL-elemzés elkészült. Elemzett utasítások: {StatementsForSummary.Count}. Találatok: {Suggestions.Count}.");
                    }
                    else
                        ReportMessage($"Hiba az SQL feldolgozásakor: {summary.ErrorMessage}");
                }

                if (planVersion == _planDocumentVersion && plans.Length > 0)
                {
                    foreach (var plan in plans)
                        PlansForSummary.Add(plan);
                    foreach (var finding in planFindings)
                        PlanSuggestions.Add(finding);

                    SelectedSummaryPlan = PlansForSummary.Count == 1 ? PlansForSummary[0] : null;
                    ReportMessage($"Tervelemzés elkészült. Tervek: {plans.Length}. Találatok: {PlanSuggestions.Count}.");
                }
            }
            catch (Exception ex)
            {
                // Felületi parancshatár: a háttérfeladat hibája ne jusson az async void parancsig.
                ReportMessage($"Az elemzés nem sikerült: {ex.Message}");
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
        /// Beolvassa a kiválasztott EXPLAIN JSON-fájlt, és megjeleníti a terveket.
        /// </summary>
        private async Task OpenJsonAsync()
        {
            var path = _fileDialogService.SelectJsonFile();

            if (path is null)
                return;

            try
            {
                StatusMessage = "JSON-fájl betöltése…";
                string content = await _textFileReader.ReadAsync(path);

                if (string.IsNullOrWhiteSpace(content))
                {
                    StatusMessage = "A kiválasztott JSON-fájl üres vagy csak üres karaktereket tartalmaz.";
                    return;
                }

                LoadExplainJson(content, Path.GetFileName(path), new PlanOrigin { Source = PlanSourceKind.JsonFile, FileName = Path.GetFileName(path), CapturedAt = DateTimeOffset.UtcNow });
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

        /// <summary>
        /// Betölti az EXPLAIN JSON terveit, és törli a korábbi tervelemzés eredményeit.
        /// Az elemzést nem indítja el.
        /// </summary>
        private void LoadExplainJson(string json, string sourceName, PlanOrigin? origin = null)
        {
            var plans = _planService.ParsePlans(json, origin);
            var roots = plans.Select(plan => plan.Root).ToList();

            _loadedExplainJson = json;
            _loadedPlans = plans;
            _planDocumentVersion++;

            SelectedSummaryPlan = null;
            ClearSummaryPair();
            PlansForSummary.Clear();
            PlanSuggestions.Clear();

            SelectedNode = null;
            PlanRoots = roots;

            ReportMessage($"Betöltve: {sourceName} ({plans.Count} terv). A vizsgálathoz indítsd el az Elemzést.");
        }

        /// <summary>
        /// Kiválasztja a tervlekéréshez használandó SQL-utasítást.
        /// </summary>
        private (string Sql, int StatementIndex) GetSqlForExplain()
        {
            if (string.IsNullOrWhiteSpace(SqlText))
                throw new InvalidOperationException("Először tölts be egy SQL-fájlt.");

            var summary = _queryService.ParseSummary(SqlText);

            if (!summary.IsSuccess)
                throw new InvalidOperationException($"Az SQL nem dolgozható fel: {summary.ErrorMessage}");

            var selected = summary.Statements.Count == 1 ? summary.Statements[0] : SelectedSummaryStatement;

            if (selected is null || (summary.Statements.Count != 1 && (_analyzedSqlText != SqlText || !StatementsForSummary.Contains(selected))))
            {
                throw new InvalidOperationException(
                    "A fájl több utasítást tartalmaz. Indítsd el az Elemzést, majd az Összegzés fülön válaszd ki a lekérdezendő SQL-utasítást.");
            }

            if (selected.TextStart is not int start || start < 0 || start > SqlText.Length ||
                selected.TextLength <= 0 || selected.TextLength > SqlText.Length - start)
            {
                throw new InvalidOperationException("A kiválasztott SQL-utasítás szöveghatárai érvénytelenek. Elemezd újra a fájlt.");
            }

            return (SqlText.Substring(start, selected.TextLength), selected.StatementIndex);
        }

        /// <summary>
        /// Becsült vagy mért végrehajtási tervet kér le, majd elemzés nélkül betölti.
        /// </summary>
        private async Task FetchPlanAsync(bool analyze)
        {
            if (!_databaseSession.HasConfiguration) 
            {
                MessageBox.Show("Először válassz adatbázis-kapcsolatot az Adatbázis-kapcsolat menüben.", "Nincs kiválasztott kapcsolat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isFetchingExplain)
                return;

            if (!_databaseSession.HasConfiguration)
            {
                ReportMessage("Először válassz adatbázis-kapcsolatot, és kattints a Kapcsolat használata gombra.");
                return;
            }

            IsFetchingExplain = true;

            int sqlVersion = _sqlDocumentVersion;
            int planVersion = _planDocumentVersion;
            string sqlSnapshot = SqlText;

            try
            {
                var request = GetSqlForExplain();
                string sql = request.Sql;
                var connection = _databaseSession.GetConnectionDetails();
                var origin = new PlanOrigin
                {
                    Source = PlanSourceKind.Database,
                    Mode = analyze ? PlanCaptureMode.Actual : PlanCaptureMode.Estimated,
                    CapturedAt = DateTimeOffset.UtcNow,
                    SqlText = sql,
                    DocumentHash = PlanOriginMatcher.HashDocument(sqlSnapshot),
                    StatementIndex = request.StatementIndex,
                    Host = connection.Options.Host,
                    Port = connection.Options.Port,
                    Database = connection.Options.Database,
                    Schema = connection.Options.Schema,
                    Username = connection.Options.Username
                };

                ReportMessage(analyze
                    ? "SQL végrehajtása és mért terv lekérése…"
                    : "Becsült végrehajtási terv lekérése…");

                string json = analyze
                    ? await _databaseExplainService.GetActualPlanAsync(connection.Options, connection.Password, sql)
                    : await _databaseExplainService.GetEstimatedPlanAsync(connection.Options, connection.Password, sql);

                if (sqlVersion != _sqlDocumentVersion || planVersion != _planDocumentVersion || sqlSnapshot != SqlText)
                {
                    ReportMessage("A tervlekérés közben megváltozott a betöltött SQL vagy terv. A visszakapott tervet nem töltöttük be.");
                    return;
                }

                string planType = analyze ? "mért terv" : "becsült terv";
                LoadExplainJson(json, $"adatbázis: {connection.Options.Database} – {planType}", origin);
            }
            catch (PostgresException ex)
            {
                ReportMessage($"PostgreSQL-hiba ({ex.SqlState}): {ex.MessageText}");
            }
            catch (NpgsqlException ex) when (ex.InnerException is TimeoutException)
            {
                ReportMessage("A tervlekérés túllépte az időkorlátot. A korábban betöltött terv megmaradt.");
            }
            catch (NpgsqlException)
            {
                ReportMessage("Adatbázis-kapcsolati hiba. Ellenőrizd a szerver elérhetőségét és a kapcsolat beállításait.");
            }
            catch (ArgumentException ex)
            {
                ReportMessage(ex.Message);
            }
            catch (FormatException ex)
            {
                ReportMessage($"Az SQL formátuma hibás: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                ReportMessage(ex.Message);
            }
            catch (JsonException)
            {
                ReportMessage("Az adatbázis válaszát nem sikerült végrehajtási tervként feldolgozni.");
            }
            catch (OperationCanceledException)
            {
                ReportMessage("A tervlekérés megszakadt.");
            }
            catch (Exception)
            {
                ReportMessage("Nem sikerült lekérni a tervet. Ellenőrizd a kapcsolatot, az SQL-ben szereplő objektumokat és a jogosultságokat. Időtúllépés is történhetett.");
            }
            finally
            {
                IsFetchingExplain = false;
            }
        }
    }
}

#endregion
