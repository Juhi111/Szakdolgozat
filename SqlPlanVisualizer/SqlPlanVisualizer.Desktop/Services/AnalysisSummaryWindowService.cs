using System.Windows;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.ViewModels;
using SqlPlanVisualizer.Desktop.Views;

namespace SqlPlanVisualizer.Desktop.Services
{
    /// <summary>
    /// Külön ablakban megjeleníti az elkészült elemzési összegzést.
    /// </summary>
    public class AnalysisSummaryWindowService
    {
        public void Show(CombinedAnalysisResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var viewModel = new AnalysisSummaryViewModel(result);
            var window = new AnalysisSummaryWindow(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            window.Show();
        }
    }
}