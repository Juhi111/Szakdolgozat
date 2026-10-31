using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.Rules;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
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
        /// A PostgreSQL tervcsomópontokat a felület által használt modellre alakítja.
        /// </summary>
        private readonly PostgreSqlPlanMapper _planMapper = new();

        /// <summary>
        /// A megjelenített végrehajtási tervek gyökércsomópontjait tárolja.
        /// </summary>
        private List<PlanNode> _planRoots = new();

        // A megjelenített SQL-szöveg és állapotüzenet háttérmezői.
        private string sqlText = string.Empty;
        private string statusMessage = "Nincs betöltött fájl";

        /// <summary>
        /// A legutóbb sikeresen feldolgozott EXPLAIN JSON terveit tárolja.
        /// </summary>
        private List<PostgreSqlExplainDto> _loadedPlans = new();

        /// <summary>
        /// Az SQL- és JSON-fájlok kiválasztására szolgáló ablakokat nyitja meg.
        /// </summary>
        private readonly FileDialogService _fileDialogService = new();

        /// <summary>
        /// Feldolgozza és ellenőrzi a PostgreSQL EXPLAIN JSON tartalmát.
        /// </summary>
        private readonly PostgreSqlPlanParser _postgreSqlPlanParser = new();

        /// <summary>
        /// Aszinkron módon beolvassa a kiválasztott fájl szövegét.
        /// </summary>
        private readonly TextFileReader _textFileReader = new();

        /// <summary>
        /// A végrehajtási terv fájában kijelölt csomópontot tárolja.
        /// </summary>
        private PlanNode? _selectedNode;

        // A felület parancsai: SQL-elemzés, SQL- és JSON-betöltés, valamint felületürítés.
        public ICommand AnalyzeCommand { get; }
        public ICommand OpenSqlCommand { get; }
        public ICommand OpenJsonCommand { get; }
        public ICommand DeleteContent { get; }

        /// <summary>
        /// Az Üzenetek fülön megjelenített, időbélyeggel ellátott naplóbejegyzések.
        /// </summary>
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        private readonly SqlAnalysisService _analysisService;
        private readonly PlanAnalysisService _planAnalysisService;

        /// <summary>
        /// A Javaslatok fülön megjelenített elemzési találatok.
        /// </summary>
        public ObservableCollection<SqlAnalysisFinding> Suggestions { get; } = new();

        /// <summary>
        /// A végrehajtási tervek elemzéséből származó találatok.
        /// </summary>
        public ObservableCollection<PlanAnalysisFinding> PlanSuggestions { get; } = new();

        /// <summary>
        /// Létrehozza a főablak parancsait, és hozzárendeli a végrehajtandó műveleteket.
        /// </summary>
        public MainViewModel(AppSettings settings)
        {
            if (settings is null)
                throw new ArgumentNullException(nameof(settings));

            _analysisService = new SqlAnalysisService(new ISqlAnalysisRule[]
            {
                new SelectStarRule(),
                new DistinctRule(),
                new NullComparisonRule(),
                new LimitWithoutOrderRule(),
                new LikeWithoutWildcardRule(),
                new LeadingWildcardSearchRule(),
                new FunctionOnColumnRule(),
                new CastOnColumnRule(),
                new ArithmeticOnColumnRule(),
                new NotInSubqueryRule(),
                new DateCastInPredicateRule(),
                new YearExtractionInPredicateRule(),
                new ConcatenationInPredicateRule(),
                new TextCastInPredicateRule(),
                new OptionalParameterFilterRule(),
                new CaseInPredicateRule(),
                new NotInWithNullLiteralRule(),
                new CountForExistenceRule(),
                new RowFilterInHavingRule(),
                new PossibleCartesianProductRule(),
                new LargeOffsetRule(threshold: settings.SqlAnalysis.LargeOffsetThreshold),
                new UnionDeduplicationRule(),
                new NaturalJoinUsageRule(),
                new RandomOrderingRule()
            });

            var rowSettings = settings.PlanAnalysis.RowEstimateMismatch;
            var filterSettings = settings.PlanAnalysis.HighFilterRemoval;
            var heapSettings = settings.PlanAnalysis.HighHeapFetches;
            var repeatedScanSettings = settings.PlanAnalysis.RepeatedSequentialScan;
            var sortLimitSettings = settings.PlanAnalysis.LargeSortForLimit;

            _planAnalysisService = new PlanAnalysisService(new IPlanAnalysisRule[] {
                new RowEstimateMismatchRule(ratioThreshold: rowSettings.RatioThreshold, differenceThreshold: rowSettings.DifferenceThreshold),
                new SortSpillRule(),
                new HashBatchRule(),
                new HashAggregateSpillRule(),
                new HighFilterRemovalRule(minimumRemovedRows: filterSettings.MinimumRemovedRows, removalRatioThreshold: filterSettings.RemovalRatioThreshold),
                new LossyBitmapHeapScanRule(),
                new HighHeapFetchesRule(minimumHeapFetches: heapSettings.MinimumHeapFetches),
                new ParallelWorkerShortageRule(),
                new RepeatedSequentialScanRule(minimumLoops: repeatedScanSettings.MinimumLoops, minimumExaminedRows: repeatedScanSettings.MinimumExaminedRows),
                new LargeSortForLimitRule(minimumInputRows: sortLimitSettings.MinimumInputRows, inputOutputRatioThreshold: sortLimitSettings.InputOutputRatioThreshold),
                new IntermediateStorageSpillRule()
            });

            OpenSqlCommand = new AsyncRelayCommand(OpenSqlAsync);
            OpenJsonCommand = new AsyncRelayCommand(OpenJsonAsync);
            AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync);
            DeleteContent = new AsyncRelayCommand(DeleteWindow);
        }

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
        public string SqlText {
            get {return sqlText;} 
            set {
                if (value == sqlText)
                    return;
                sqlText = value;
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

            PlanSuggestions.Clear();

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
            Suggestions.Clear();

            StatusMessage = "SQL feldolgozása…";

            string sql = SqlText;
            int documentVersion = _sqlDocumentVersion;

            var summary = await Task.Run(() => _queryService.ParseSummary(sql));

            if (documentVersion != _sqlDocumentVersion)
                return;

            if (summary.IsSuccess)
            {
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
            Suggestions.Clear();

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
                    var plans = _postgreSqlPlanParser.Parse(content);
                    _loadedPlans = plans;
                    StatusMessage = $"Betöltve: {Path.GetFileName(path)} ({plans.Count} plan)";

                    List<PlanNode> roots = new();
                    foreach (var plan in plans)
                    {
                        if (plan.Plan != null)
                        {
                            var rootNode = _planMapper.MapNode(plan.Plan);
                            roots.Add(rootNode);
                        }
                    }
                    SelectedNode = null;
                    PlanRoots = roots;
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
    }
}
