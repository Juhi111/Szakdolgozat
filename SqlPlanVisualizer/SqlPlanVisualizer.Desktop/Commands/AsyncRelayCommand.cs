using System.Windows.Input;

namespace SqlPlanVisualizer.Desktop.Commands
{

    // Az AsyncRelayCommand osztály, amely implementálja az ICommand interfészt, és lehetővé teszi aszinkron műveletek végrehajtását
    internal class AsyncRelayCommand : ICommand //Microsoft ajánlás https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand
    {
        // A művelet, amelyet a parancs végrehajt
        private readonly Func<Task> _execute;
        // A jelző, amely azt jelzi, hogy a parancs végrehajtás alatt áll
        private bool _isExecuting;

        // Konstruktor, amely inicializálja a parancsot a megadott művelettel
        public AsyncRelayCommand(Func<Task> execute)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            _execute = execute;            
        }

        // Az ICommand interfész CanExecuteChanged eseményének implementációja
        public event EventHandler? CanExecuteChanged;

        // Az ICommand interfész CanExecute metódusának implementációja
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting;
        }

        // A CanExecuteChanged esemény kiváltása, amely jelzi, hogy a parancs végrehajthatósága megváltozott
        private void RaiseCanExecuteChanged() { 
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        // Az ICommand interfész Execute metódusának implementációja, amely aszinkron módon végrehajtja a parancsot
        public async void Execute(object? parameter) {

            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            finally { 
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}
