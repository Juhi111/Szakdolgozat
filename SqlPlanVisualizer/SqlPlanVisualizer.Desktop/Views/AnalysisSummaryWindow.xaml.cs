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
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}