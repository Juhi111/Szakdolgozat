using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.Configuration;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Desktop.ViewModels;
using SqlPlanVisualizer.Desktop.Views;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SqlPlanVisualizer.Desktop
{    
    public partial class MainWindow : Window
    {
        private readonly DatabaseSession _databaseSession = new();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                var settings = AppSettingsLoader.Load();
                DataContext = new MainViewModel(settings);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is JsonException)
            {
                MessageBox.Show(
                    $"Nem sikerült betölteni a konfigurációt:\n{ex.Message}",
                    "Konfigurációs hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Kijelöli az összegzéshez kiválasztott teljes SQL-utasítást, és odagörget.
        /// </summary>
        private void SummaryStatementComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SqlTextBox.SelectionLength = 0;

            if (sender is not System.Windows.Controls.ComboBox comboBox || comboBox.SelectedItem is not SqlStatementSummary statement || statement.TextStart is not int start)
            {
                return;
            }

            int length = statement.TextLength;
            string text = SqlTextBox.Text;

            if (start < 0 || start > text.Length ||
                length <= 0 || length > text.Length - start)
            {
                return;
            }

            SqlTextBox.Focus();
            SqlTextBox.Select(start, length);

            int lineIndex = SqlTextBox.GetLineIndexFromCharacterIndex(start);

            if (lineIndex >= 0)
                SqlTextBox.ScrollToLine(lineIndex);
        }

        /// <summary>
        /// A végrehajtási terv fájában megváltozott kijelölést
        /// továbbítja a MainViewModel SelectedNode tulajdonságának.
        /// Ezzel adatbindingon keresztül frissül a részletező panel.
        /// </summary>
        /// <param name="sender">Az eseményt kiváltó TreeView.</param>
        /// <param name="e">
        /// A kijelölés változásának adatai.
        /// A NewValue az újonnan kijelölt adatobjektum.
        /// </param>
        private void PlanTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SelectedNode = e.NewValue as PlanNode;
            }
        }

        /// <summary>
        /// A kiválasztott javaslat forráspozíciója alapján kijelöli az érintett SQL-sort,
        /// és odagörgeti a szövegmezőt. Hiányzó forráspozíció esetén megszünteti a kijelölést.
        /// </summary>
        /// <param name="sender">Az eseményt kiváltó vezérlő.</param>
        /// <param name="e">A kijelölés változásának adatai.</param>
        private void SuggestionsListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e){
            if (SuggestionsListView.SelectedItem is not SqlAnalysisFinding finding)
            {
                return;
            }

            if (finding.SourceOffset is not int offset)
            {
                SqlTextBox.SelectionLength = 0;
                return;
            }

            if (offset < 0 || SqlTextBox.Text.Length < offset)
                return;

            int lineIndex = SqlTextBox.GetLineIndexFromCharacterIndex(offset);
            if (lineIndex < 0)
                return;

            int lineStart = SqlTextBox.GetCharacterIndexFromLineIndex(lineIndex);
            string lineText = SqlTextBox.GetLineText(lineIndex);
            
            int lineLength = lineText.TrimEnd('\r', '\n').Length;

            SqlTextBox.Focus();
            SqlTextBox.Select(lineStart, lineLength);
            SqlTextBox.ScrollToLine(lineIndex);
        }

        /// <summary>
        /// A kiválasztott tervjavaslathoz tartozó csomópontot megjeleníti a részletező panelen.
        /// </summary>
        private void PlanSuggestionsListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && PlanSuggestionsListView.SelectedItem is PlanAnalysisFinding finding)
            {
                viewModel.SelectedNode = finding.Node;
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new SqlPlanVisualizer.Desktop.Views.SettingsWindow
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new SqlPlanVisualizer.Desktop.Views.AnalysisHistoryWindow
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void DatabaseConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var service = new PostgreSqlConnectionService();

            var viewModel = new DatabaseConnectionViewModel(
                service,
                _databaseSession);

            var window = new DatabaseConnectionWindow(viewModel)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                var details = _databaseSession.GetConnectionDetails();

                MessageBox.Show(
                    this,
                    $"Kiválasztott adatbázis: {details.Options.Database}\n" +
                    $"Szerver: {details.Options.Host}:{details.Options.Port}",
                    "Adatbázis-kapcsolat",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ShowDatabaseConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_databaseSession.HasConfiguration)
            {
                MessageBox.Show(this, "Nincs kiválasztott adatbázis-kapcsolat.", "Adatbázis-kapcsolat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var options = _databaseSession.GetConnectionDetails().Options;

            string message = $"Szerver: {options.Host}:{options.Port}\n" +
                             $"Adatbázis: {options.Database}\n" +
                             $"Felhasználó: {options.Username}\n" +
                             $"Séma: {options.Schema}\n\n" +
                             "Ezek a kiválasztott beállítások; a szerver aktuális elérhetőségét most nem ellenőriztük.";

            MessageBox.Show(this, message, "Kiválasztott kapcsolat", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ForgetDatabaseConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_databaseSession.HasConfiguration)
            {
                MessageBox.Show(this, "Nincs elfelejthető adatbázis-kapcsolat.", "Adatbázis-kapcsolat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _databaseSession.Clear();

            MessageBox.Show(this, "A kapcsolati beállításokat elfelejtettük.", "Adatbázis-kapcsolat", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}