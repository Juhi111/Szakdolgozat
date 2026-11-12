using SqlPlanVisualizer.Core.Models.History;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    public class AnalysisHistoryViewModel : ViewModelBase
    {
        private readonly UserSettingsService settingsService = new();
        private readonly AnalysisHistoryService historyService = new();
        private readonly PdfExportService pdfExportService = new();
        public ICommand ExportPdfCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand DeleteAllCommand { get; }
        private AnalysisHistoryEntry? selectedEntry;
        private string statusMessage = "Előzmények betöltése…";

        public AnalysisHistoryViewModel()
        {
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
            DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync);
            DeleteAllCommand = new AsyncRelayCommand(DeleteAllAsync);
        }

        private async Task ExportPdfAsync()
        {
            var entry = SelectedEntry;

            if (entry is null)
            {
                StatusMessage = "A PDF-exporthoz válassz egy mentett elemzést.";
                return;
            }

            await pdfExportService.ExportHistoryToPdfAsync(entry);
        }

        public ObservableCollection<AnalysisHistoryEntry> Entries { get; } = new();
        public ObservableCollection<string> Errors { get; } = new();

        public AnalysisHistoryEntry? SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (selectedEntry == value)
                    return;

                selectedEntry = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => statusMessage;
            private set
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadAsync()
        {
            Entries.Clear();
            Errors.Clear();
            SelectedEntry = null;
            StatusMessage = "Előzmények betöltése…";

            try
            {
                var settings = await settingsService.LoadAsync();
                var result = await historyService.LoadAllAsync(settings.HistoryDirectory);

                foreach (var entry in result.Entries)
                    Entries.Add(entry);

                foreach (string error in result.Errors)
                    Errors.Add(error);

                SelectedEntry = Entries.Count > 0 ? Entries[0] : null;

                StatusMessage = $"Betöltött előzmények: {Entries.Count}. Hibás fájlok: {Errors.Count}.";
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or
                JsonException or ArgumentException or NotSupportedException)
            {
                StatusMessage = "Nem sikerült betölteni az előzményeket.";
                Errors.Add(ex.Message);
            }
        }

        /// <summary>
        /// Megerősítés után törli a kiválasztott előzményt,
        /// majd frissíti a listát.
        /// </summary>
        private async Task DeleteSelectedAsync()
        {
            var entry = SelectedEntry;

            if (entry is null)
                return;

            var confirmation = MessageBox.Show(
                $"Törlöd a(z) {entry.CreatedAt:yyyy.MM.dd. HH:mm:ss} időpontban " +
                "mentett elemzést?\n\nA törlés nem vonható vissza.",
                "Előzmény törlése",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                var settings = await settingsService.LoadAsync();

                bool deleted = await historyService.DeleteAsync(
                    entry.Id,
                    settings.HistoryDirectory);

                await LoadAsync();

                StatusMessage = deleted
                    ? "A kijelölt előzmény törölve."
                    : "A bejegyzés fájlja már nem található. A lista frissítve.";
            }
            catch (Exception ex) when (
                ex is IOException or
                UnauthorizedAccessException or
                JsonException or
                ArgumentException or
                NotSupportedException)
            {
                StatusMessage = $"Nem sikerült törölni az előzményt: {ex.Message}";
            }
        }

        /// <summary>
        /// Megerősítés után törli a beállított mappa érvényes előzményeit,
        /// majd frissíti a listát és megjeleníti az esetleges hibákat.
        /// </summary>
        private async Task DeleteAllAsync()
        {
            try
            {
                var settings = await settingsService.LoadAsync();
                string directory = settings.HistoryDirectory;

                // A megerősítéshez az aktuális fájlok számát használjuk.
                var history = await historyService.LoadAllAsync(directory);

                if (history.Entries.Count == 0)
                {
                    StatusMessage = "Nincs törölhető elemzési előzmény.";
                    return;
                }

                var confirmation = MessageBox.Show(
                    $"Törlöd az összes mentett elemzést?\n\n" +
                    $"Bejegyzések száma: {history.Entries.Count}\n" +
                    $"Mappa: {directory}\n\n" +
                    "A törlés nem vonható vissza. " +
                    "Más fájlok és hibás előzmények megmaradnak.",
                    "Összes előzmény törlése",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (confirmation != MessageBoxResult.Yes)
                    return;

                var result = await historyService.DeleteAllAsync(directory);

                await LoadAsync();

                foreach (string error in result.Errors)
                {
                    Errors.Add(error);
                }

                StatusMessage = result.Errors.Count == 0
                    ? $"Törölt előzmények száma: {result.DeletedCount}."
                    : $"Törölt előzmények száma: {result.DeletedCount}. " +
                      $"Hibák vagy kihagyott fájlok: {result.Errors.Count}. " +
                      "Részletek a Betöltési hibák fülön.";
            }
            catch (Exception ex) when (
                ex is IOException or
                UnauthorizedAccessException or
                JsonException or
                ArgumentException or
                NotSupportedException)
            {
                StatusMessage = $"Nem sikerült törölni az előzményeket: {ex.Message}";
            }
        }
    }
}