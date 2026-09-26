using System.Windows;
using SqlPlanVisualizer.Desktop.ViewModels;

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
    }
}