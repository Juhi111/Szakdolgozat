using System.Windows;
using SqlPlanVisualizer.Desktop.ViewModels;

namespace SqlPlanVisualizer.Desktop.Views
{
    public partial class AnalysisSummaryWindow : Window
    {
        public AnalysisSummaryWindow(AnalysisSummaryViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            DataContext = viewModel;
            Closed += (_, _) => viewModel.RewriteComparison?.Cancel();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}