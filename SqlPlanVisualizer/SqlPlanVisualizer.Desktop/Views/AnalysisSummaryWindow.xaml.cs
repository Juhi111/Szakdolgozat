using System.Windows;
using SqlPlanVisualizer.Desktop.ViewModels;

namespace SqlPlanVisualizer.Desktop.Views
{
    public partial class AnalysisSummaryWindow : Window
    {
        private SqlRewriteWindow? _rewriteWindow;

        public AnalysisSummaryWindow(AnalysisSummaryViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            DataContext = viewModel;
            Closed += (_, _) => viewModel.RewriteComparison?.Cancel();
        }

        private void OpenRewrites_Click(object sender, RoutedEventArgs e)
        {
            if (_rewriteWindow is not null) { _rewriteWindow.Activate(); return; }
            _rewriteWindow = new SqlRewriteWindow((AnalysisSummaryViewModel)DataContext) { Owner = this };
            _rewriteWindow.Closed += (_, _) => _rewriteWindow = null;
            _rewriteWindow.Show();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}