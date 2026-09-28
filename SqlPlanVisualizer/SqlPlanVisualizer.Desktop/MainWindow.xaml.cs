using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.ViewModels;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos;
using System.Windows;

namespace SqlPlanVisualizer.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel{};
        }

        /// <summary>
        /// A végrehajtási terv fájában megváltozott kijelölést
        /// továbbítja a MainViewModel SelectedNode tulajdonságának.
        /// Ezzel adatbindingon keresztül frissül a részletező panel.
        /// </summary>
        /// <param name="sender">Az eseményt kiváltó TreeView.</param>
        /// <param name="e">
        /// A kijelölés változásának adatai.
        /// A NewValue az újonnan kijelölt adatobjektum.
        /// </param>
        private void PlanTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SelectedNode = e.NewValue as PlanNode;
            }
        }
    }
}