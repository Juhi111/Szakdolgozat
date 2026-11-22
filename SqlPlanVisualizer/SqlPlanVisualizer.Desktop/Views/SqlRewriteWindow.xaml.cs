using System.Windows;
using SqlPlanVisualizer.Desktop.ViewModels;

namespace SqlPlanVisualizer.Desktop.Views;

public partial class SqlRewriteWindow : Window
{
    public SqlRewriteWindow(AnalysisSummaryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closed += (_, _) => viewModel.RewriteComparison?.Cancel();
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
