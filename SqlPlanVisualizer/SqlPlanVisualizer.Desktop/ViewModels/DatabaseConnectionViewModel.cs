using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Desktop.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    public class DatabaseConnectionViewModel : ViewModelBase
    {
        private readonly IDatabaseConnectionService _databaseConnectionService;
        private string _statusMessage = "Nincs tesztelt kapcsolat.";
        private bool _isBusy;
        private readonly DatabaseSession _databaseSession;
        private readonly IDatabaseConnectionProfileService _profileService;
        private DatabaseConnectionProfile? _selectedProfile;
        private string _profileName = string.Empty;
        private bool _profilesLoaded;

        public ObservableCollection<DatabaseConnectionProfile> Profiles { get; } = new();
        public bool CanManageProfiles => _profilesLoaded && !IsBusy;
        public bool CanDeleteProfile => CanManageProfiles && SelectedProfile is not null;

        public string ProfileName
        {
            get => _profileName;
            set { _profileName = value; OnPropertyChanged(); }
        }

        public DatabaseConnectionProfile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile == value)
                    return;
                _selectedProfile = value;
                if (value is not null)
                {
                    Options = value.Options.Copy();
                    ProfileName = value.Name;
                    OnPropertyChanged(nameof(Options));
                    StatusMessage = "Kapcsolat betöltve. Add meg a jelszót, majd válaszd a Kapcsolat használata gombot.";
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDeleteProfile));
            }
        }

        public async Task LoadProfilesAsync()
        {
            if (IsBusy)
                return;
            IsBusy = true;
            _profilesLoaded = false;
            try
            {
                var profiles = await _profileService.LoadAsync();
                SelectedProfile = null;
                Profiles.Clear();
                foreach (var profile in profiles)
                    Profiles.Add(profile);
                _profilesLoaded = true;
                StatusMessage = "Válassz mentett kapcsolatot, vagy add meg egy új kapcsolat adatait.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                StatusMessage = "A mentett kapcsolatok nem tölthetők be. Ellenőrizd a profilfájlt és a hozzáférést, majd próbáld újra. A fájlt nem módosítottuk.";
            }
            finally { IsBusy = false; }
        }

        public void NewProfile()
        {
            if (!CanManageProfiles)
                return;
            SelectedProfile = null;
            ProfileName = string.Empty;
            Options = new();
            OnPropertyChanged(nameof(Options));
            StatusMessage = "Add meg az új kapcsolat nevét és adatait. A mentéshez nem szükséges jelszó.";
        }

        public async Task SaveProfileAsync()
        {
            if (!CanManageProfiles)
                return;
            IsBusy = true;
            try
            {
                var profile = new DatabaseConnectionProfile
                {
                    Id = SelectedProfile?.Id ?? Guid.NewGuid(),
                    Name = ProfileName.Trim(),
                    Options = Options.Copy()
                };
                await _profileService.SaveAsync(profile);
                var previous = Profiles.FirstOrDefault(item => item.Id == profile.Id);
                if (previous is not null)
                    Profiles.Remove(previous);
                Profiles.Add(profile);
                SelectedProfile = profile;
                StatusMessage = $"A(z) „{profile.Name}” kapcsolat elmentve, jelszó nélkül. A mentés nem végez kapcsolattesztet.";
            }
            catch (ArgumentException ex) { StatusMessage = ex.Message; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                StatusMessage = "Nem sikerült menteni a kapcsolatot. Ellenőrizd a profilfájlt és a hozzáférést. A korábbi mentést nem írtuk felül hibás adatokkal.";
            }
            finally { IsBusy = false; }
        }

        public async Task DeleteProfileAsync()
        {
            if (!CanDeleteProfile)
                return;
            var profile = SelectedProfile!;
            IsBusy = true;
            try
            {
                await _profileService.DeleteAsync(profile.Id);
                SelectedProfile = null;
                Profiles.Remove(profile);
                ProfileName = string.Empty;
                Options = new();
                OnPropertyChanged(nameof(Options));
                StatusMessage = "A mentett profil törölve. A használatra kiválasztott kapcsolat változatlan maradt.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                StatusMessage = "Nem sikerült törölni a profilt. Ellenőrizd a profilfájlt és a hozzáférést.";
            }
            finally { IsBusy = false; }
        }

        public DatabaseConnectionViewModel(IDatabaseConnectionService databaseConnectionService, DatabaseSession databaseSession, IDatabaseConnectionProfileService profileService)
        {
            ArgumentNullException.ThrowIfNull(databaseConnectionService);
            ArgumentNullException.ThrowIfNull(databaseSession);

            _databaseConnectionService = databaseConnectionService;
            _databaseSession = databaseSession;
            ArgumentNullException.ThrowIfNull(profileService);
            _profileService = profileService;

            if (_databaseSession.HasConfiguration)
            {
                Options = _databaseSession.GetConnectionDetails().Options;
                StatusMessage = "A korábban kiválasztott kapcsolat beállításai betöltve. Új ellenőrzéshez add meg a jelszót.";
            }
        }

        public DatabaseConnectionOptions Options { get; private set; } = new();

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
                OnPropertyChanged(nameof(CanManageProfiles));
                OnPropertyChanged(nameof(CanDeleteProfile));
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
                var options = Options.Copy();

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