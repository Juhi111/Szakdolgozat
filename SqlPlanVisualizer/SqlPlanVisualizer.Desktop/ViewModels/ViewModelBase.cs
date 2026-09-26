using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    // A ViewModelBase osztály, amely implementálja az INotifyPropertyChanged interfészt
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        // Az esemény, amely jelzi, hogy egy tulajdonság értéke megváltozott
        public event PropertyChangedEventHandler? PropertyChanged;

        // A metódus, amely meghívja a PropertyChanged eseményt a megadott tulajdonság nevével
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
