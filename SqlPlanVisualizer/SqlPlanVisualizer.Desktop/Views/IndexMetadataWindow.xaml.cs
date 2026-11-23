using System.Windows;
using SqlPlanVisualizer.Desktop.ViewModels;
namespace SqlPlanVisualizer.Desktop.Views;
public partial class IndexMetadataWindow : Window
{
    public IndexMetadataWindow(IndexMetadataViewModel viewModel)
    {
        InitializeComponent(); DataContext = viewModel;
        Closed += (_, _) => viewModel.Cancel();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => ((IndexMetadataViewModel)DataContext).Cancel();
}
