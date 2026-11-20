using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Desktop.Services;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    public class DatabaseConnectionViewModel : ViewModelBase
    {
        private readonly IDatabaseConnectionService _databaseConnectionService;
        private string _statusMessage = "Nincs tesztelt kapcsolat.";
        private bool _isBusy;
        private readonly DatabaseSession _databaseSession;

        public DatabaseConnectionViewModel(IDatabaseConnectionService databaseConnectionService, DatabaseSession databaseSession)
        {
            ArgumentNullException.ThrowIfNull(databaseConnectionService);
            ArgumentNullException.ThrowIfNull(databaseSession);

            _databaseConnectionService = databaseConnectionService;
            _databaseSession = databaseSession;

            if (_databaseSession.HasConfiguration)
            {
                Options = _databaseSession.GetConnectionDetails().Options;
                StatusMessage = "A korábban kiválasztott kapcsolat beállításai betöltve. Új ellenőrzéshez add meg a jelszót.";
            }
        }

        public DatabaseConnectionOptions Options { get; } = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value)
                    return;

                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value)
                    return;

                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public Task<bool> TestConnectionAsync(string password, CancellationToken cancellationToken = default)
        {
            return CheckConnectionAsync(password, false, cancellationToken);
        }

        public Task<bool> UseConnectionAsync(string password, CancellationToken cancellationToken = default)
        {
            return CheckConnectionAsync(password, true, cancellationToken);
        }

        private async Task<bool> CheckConnectionAsync(string password, bool useConnection,
            CancellationToken cancellationToken)
        {
            if (IsBusy)
                return false;

            IsBusy = true;
            StatusMessage = "Kapcsolat ellenőrzése…";

            try
            {
                // Pontosan azokat az adatokat tároljuk majd, amelyeket ellenőriztünk.
                var options = new DatabaseConnectionOptions
                {
                    Host = Options.Host,
                    Port = Options.Port,
                    Database = Options.Database,
                    Username = Options.Username,
                    Schema = Options.Schema,
                    TlsMode = Options.TlsMode
                };

                var info = await _databaseConnectionService.TestConnectionAsync(options, password, cancellationToken);

                // Bezárt ablak esetén ne fogadjuk el utólag a kapcsolatot.
                cancellationToken.ThrowIfCancellationRequested();

                if (useConnection)
                {
                    _databaseSession.Set(options, password);
                }

                StatusMessage = $"Sikeres kapcsolódás! Adatbázis: {info.DatabaseName}, felhasználó: {info.Username}, PostgreSQL-verzió: {info.ServerVersion}.";

                return true;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                StatusMessage = "A kapcsolatteszt megszakítva.";
            }
            catch (ArgumentException ex)
            {
                StatusMessage = $"Hibás kapcsolati beállítás: {ex.Message}";
            }
            catch (Exception)
            {
                StatusMessage =
                    "Nem sikerült kapcsolódni. Ellenőrizd a szerver elérhetőségét, " +
                    "a bejelentkezési adatokat és a TLS-beállítást.";
            }
            finally
            {
                IsBusy = false;
            }

            return false;
        }
    }
}