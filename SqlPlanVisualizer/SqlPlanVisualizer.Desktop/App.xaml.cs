using System.Configuration;
using System.Data;
using System.Windows;

namespace SqlPlanVisualizer.Desktop
{    
    public partial class App : Application
    {
        public App()
        {
            AppContext.SetSwitch("Switch.System.Windows.Controls.Text.UseAdornerForTextboxSelectionRendering", false);
        }
    }
}
