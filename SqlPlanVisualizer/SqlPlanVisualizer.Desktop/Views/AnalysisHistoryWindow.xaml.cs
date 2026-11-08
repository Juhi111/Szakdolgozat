using System.Windows;
using SqlPlanVisualizer.Desktop.ViewModels;

namespace SqlPlanVisualizer.Desktop.Views
{
    public partial class AnalysisHistoryWindow : Window
    {
        private readonly AnalysisHistoryViewModel viewModel = new();

        public AnalysisHistoryWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Window_Loaded;
            await viewModel.LoadAsync();
        }
    }
}