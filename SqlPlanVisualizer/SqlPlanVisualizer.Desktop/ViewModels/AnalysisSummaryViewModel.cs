using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using System.Windows;
using System.Windows.Input;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    /// <summary>
    /// Egy elkészült összegzés adatait biztosítja az eredményablaknak.
    /// </summary>
    public class AnalysisSummaryViewModel
    {

        public ICommand SaveResultToPdf { get; }

        public AnalysisSummaryViewModel(CombinedAnalysisResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            Result = result;
            SaveResultToPdf = new AsyncRelayCommand(SaveToPdf);
        }

        public CombinedAnalysisResult Result { get; }

        public string PairDescription =>
            $"{Result.Pair.Statement.StatementIndex}. SQL-utasítás – " +
            $"{Result.Pair.Plan.PlanIndex}. végrehajtási terv";

        public string PlanningTimeText => Result.PlanningTime is double time
            ? $"{time:N2} ms"
            : "Nincs adat";

        public string ExecutionTimeText => Result.ExecutionTime is double time
            ? $"{time:N2} ms"
            : "Nincs adat";

        public string SqlFindingsHeader =>
            $"SQL-találatok ({Result.SqlFindings.Count})";

        public string PlanFindingsHeader =>
            $"Tervtalálatok ({Result.PlanFindings.Count})";

        public Task SaveToPdf()
        {
            var savefileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "PDF mentése",
                Filter = "PDF fájl (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                FileName = "Elemzés.pdf"
            };

            bool? result = savefileDialog.ShowDialog();

            if (result == true)
            {
                var path = savefileDialog.FileName;

                var pdfService = new PdfExportService(Result, path);
                pdfService.ExportAnalysisSummaryToPdf();
            }
            else
                MessageBox.Show("Sikertelen mentés");
            

            return Task.CompletedTask;
        }
    }

}