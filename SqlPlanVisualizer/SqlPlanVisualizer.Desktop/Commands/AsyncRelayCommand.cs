using System.Windows.Input;

namespace SqlPlanVisualizer.Desktop.Commands
{
    /// <summary>
    /// Aszinkron műveletet futtató parancs, amely a végrehajtás idejére
    /// letiltja saját újbóli indítását.
    /// </summary>
    internal class AsyncRelayCommand : ICommand //Microsoft ajánlás https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand
    {
        /// <summary>
        /// A parancs indításakor végrehajtandó aszinkron művelet.
        /// </summary>
        private readonly Func<Task> _execute;

        // <summary>
        /// Jelzi, hogy a parancs végrehajtása folyamatban van-e.
        /// </summary>
        private bool _isExecuting;

        /// <summary>
        /// Létrehozza a parancsot a megadott művelettel.
        /// </summary>
        /// <param name="execute">A végrehajtandó aszinkron művelet.</param>
        /// <exception cref="ArgumentNullException">A művelet null.</exception>
        public AsyncRelayCommand(Func<Task> execute)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            _execute = execute;            
        }

        // <summary>
        /// Értesíti a parancshoz kapcsolt vezérlőket a végrehajthatóság változásáról.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Megadja, hogy a parancs elindítható-e.
        /// </summary>
        /// <param name="parameter">A parancs paramétere; ez a megvalósítás nem használja.</param>
        /// <returns>Igaz, ha a parancs jelenleg nem fut.</returns>
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting;
        }

        /// <summary>
        /// Kiváltja a végrehajthatóság változását jelző eseményt.
        /// </summary>
        private void RaiseCanExecuteChanged() { 
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Elindítja és megvárja a műveletet, ha a parancs végrehajtható.
        /// A futási állapotot hiba esetén is visszaállítja, de a kivételt nem kezeli.
        /// </summary>
        /// <param name="parameter">A parancs paramétere; ez a megvalósítás nem használja.</param>
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
