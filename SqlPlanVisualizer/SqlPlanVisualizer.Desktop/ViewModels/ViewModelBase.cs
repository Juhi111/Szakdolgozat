using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    /// <summary>
    /// A ViewModelek közös alaposztálya, amely értesíti a felületet
    /// a tulajdonságértékek változásáról.
    /// </summary>    
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        // <summary>
        /// Egy tulajdonság értékének változását jelző esemény.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Kiváltja a megadott tulajdonság változását jelző eseményt.
        /// A hívó tag nevét alapértelmezés szerint a fordító adja át.
        /// </summary>
        /// <param name="propertyName">A megváltozott tulajdonság neve.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
