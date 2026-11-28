using System.Windows;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
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
        public void Show(CombinedAnalysisResult result, string? analyzedSql = null, DatabaseSession? session = null, SqlRewriteComparisonService? comparisonService = null, SqlPlanVisualizer.Core.Analysis.SqlAnalysisService? sqlAnalysis = null)
        {
            ArgumentNullException.ThrowIfNull(result);

            var viewModel = new AnalysisSummaryViewModel(result, analyzedSql, session, comparisonService, sqlAnalysis);
            var window = new AnalysisSummaryWindow(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            window.Show();
        }
    }
}