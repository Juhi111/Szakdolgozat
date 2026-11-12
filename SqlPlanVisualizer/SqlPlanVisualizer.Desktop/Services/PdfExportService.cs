using Microsoft.Win32;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.History;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using System.IO;
using System.Windows;

namespace SqlPlanVisualizer.Desktop.Services
{
    /// <summary>A PDF-export fájlválasztását és felületi visszajelzéseit kezeli.</summary>
    public class PdfExportService
    {
        private readonly PdfService pdfService = new();

        public Task ExportAnalysisSummaryToPdfAsync(CombinedAnalysisResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return ExportAsync(path => pdfService.SaveResultToPdf(result, path));
        }

        public Task ExportHistoryToPdfAsync(AnalysisHistoryEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            return ExportAsync(path => pdfService.SaveHistoryToPdf(entry, path));
        }

        private async Task ExportAsync(Action<string> save)
        {
            try
            {
                var owner = Application.Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(window => window.IsActive)
                    ?? Application.Current.MainWindow;

                var dialog = new SaveFileDialog
                {
                    Title = "Elemzési összegzés mentése",
                    Filter = "PDF-fájlok (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = "Elemzés.pdf"
                };

                if (dialog.ShowDialog(owner) != true)
                    return;

                string path = dialog.FileName;

                await Task.Run(() => save(path));

                MessageBox.Show(
                    "Az elemzési összegzés PDF-mentése elkészült.",
                    "PDF mentése",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Nincs jogosultság a kiválasztott fájl írásához. Válassz másik mentési helyet.",
                    "PDF mentési hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    $"Nem sikerült menteni a PDF-fájlt. Ellenőrizd a mentési helyet, és hogy a fájlt nem használja-e másik program.\n\n{ex.Message}",
                    "PDF mentési hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // A PDF-generálás hibája ne jusson az async void parancsig.
                MessageBox.Show(
                    $"Nem sikerült elkészíteni a PDF-dokumentumot.\n\n{ex.Message}",
                    "PDF mentési hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}