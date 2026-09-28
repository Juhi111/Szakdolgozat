using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        //mapper példány a PostgreSQL Explain JSON csomópontok és a belső PlanNode modellek közötti konverzióhoz
        private readonly PostgreSqlPlanMapper _planMapper = new();

        //a betöltött planok gyökér csomópontjainak listája
        private List<PlanNode> _planRoots = new();

        //sql text és status message propertyk, valamint a betöltött planok listája
        private string sqlText = string.Empty;
        private string statusMessage = "Nincs betöltött fájl";
        private List<PostgreSqlExplainDto> _loadedPlans = new();

        //filereader mindkét feladatra, sql és json file olvasására
        private readonly FileDialogService _fileDialogService = new();

        //sql olvasó és feldolgozó
        private readonly SqlFileReader _sqlFileReader = new();

        //json file olvasó és feldolgozó, illetve explain plan feldolgozó
        private readonly JsonFileReader _jsonFileReader = new();
        private readonly PostgreSqlPlanParser _postgreSqlPlanParser = new();

        //treeview kiválasztott node-ja
        private PlanNode? _selectedNode;

        //command az sql és json file megnyitására
        public ICommand OpenSqlCommand { get; }
        public ICommand OpenJsonCommand { get; }

        public MainViewModel()
        {
            OpenSqlCommand = new AsyncRelayCommand(OpenSqlAsync);
            OpenJsonCommand = new AsyncRelayCommand(OpenJsonAsync);
        }

        public PlanNode? SelectedNode {
            get { return _selectedNode;}
            set {
                if (value == _selectedNode)
                    return;
                _selectedNode = value;
                OnPropertyChanged();
            }
        }

        public string SqlText {
            get {return sqlText;} 
            set {
                if (value == sqlText)
                    return;
                sqlText = value;
                OnPropertyChanged();
            }
        }

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

        // A kiválasztott SQL-fájl megnyitása és beolvasása
        private async Task OpenSqlAsync() {

            var path = _fileDialogService.SelectSqlFile();
            if (path == null) return;

            try
            {
                StatusMessage = "SQL-fájl betöltése…";
                string content = await _sqlFileReader.ReadAsync(path);
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

        private async Task OpenJsonAsync()
        {
            var path = _fileDialogService.SelectJsonFile();
            if (path == null) return;
            try
            {
                StatusMessage = "JSON-fájl betöltése…";
                string content = await _jsonFileReader.ReadAsync(path);
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
    }
}
