using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SqlPlanVisualizer.Core.Models.History;
using SqlPlanVisualizer.Infrastructure.FileSystem;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    public class AnalysisHistoryViewModel : ViewModelBase
    {
        private readonly UserSettingsService settingsService = new();
        private readonly AnalysisHistoryService historyService = new();

        private AnalysisHistoryEntry? selectedEntry;
        private string statusMessage = "Előzmények betöltése…";

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
    }
}