using System.Text.Json;
using System.Text.Json.Serialization;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    /// <summary>Kapcsolati profilokat tárol helyi JSON-fájlban. Jelszót nem fogad és nem ment.</summary>
    public sealed class DatabaseConnectionProfileService : IDatabaseConnectionProfileService
    {
        private readonly string _path;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

        public DatabaseConnectionProfileService() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SqlPlanVisualizer", "database-connections.json"))
        {
        }

        public DatabaseConnectionProfileService(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            if (!Path.IsPathFullyQualified(path))
                throw new ArgumentException("Teljes fájlútvonal szükséges.", nameof(path));
            _path = path;
        }

        public async Task<List<DatabaseConnectionProfile>> LoadAsync()
        {
            await _gate.WaitAsync();
            try { return await ReadAsync(); }
            finally { _gate.Release(); }
        }

        public async Task SaveAsync(DatabaseConnectionProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            Validate(profile);
            // Saját, jelszó nélküli pillanatkép még az első várakozás előtt.
            var snapshot = new DatabaseConnectionProfile
            {
                Id = profile.Id,
                Name = profile.Name.Trim(),
                Options = profile.Options.Copy()
            };

            await _gate.WaitAsync();
            try
            {
                var profiles = await ReadAsync();
                if (profiles.Any(item => item.Id != snapshot.Id && string.Equals(item.Name, snapshot.Name, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException("Ezzel a névvel már létezik kapcsolat. Válassz másik nevet, vagy szerkeszd a meglévőt.");

                int index = profiles.FindIndex(item => item.Id == snapshot.Id);
                if (index < 0)
                    profiles.Add(snapshot);
                else
                    profiles[index] = snapshot;

                await WriteAsync(profiles);
            }
            finally { _gate.Release(); }
        }

        public async Task DeleteAsync(Guid id)
        {
            await _gate.WaitAsync();
            try
            {
                var profiles = await ReadAsync();
                if (profiles.RemoveAll(item => item.Id == id) > 0)
                    await WriteAsync(profiles);
            }
            finally { _gate.Release(); }
        }

        private async Task<List<DatabaseConnectionProfile>> ReadAsync()
        {
            string json;
            try { json = await File.ReadAllTextAsync(_path); }
            catch (FileNotFoundException) { return new(); }
            catch (DirectoryNotFoundException) { return new(); }

            var file = JsonSerializer.Deserialize<ProfileFile>(json, JsonOptions)
                ?? throw new JsonException("A kapcsolati profilok fájlja üres.");
            if (file.FormatVersion != 1 || file.Profiles is null)
                throw new JsonException("Nem támogatott vagy hiányos kapcsolati profilfájl.");

            var ids = new HashSet<Guid>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var profile in file.Profiles)
            {
                try { Validate(profile); }
                catch (ArgumentException ex) { throw new JsonException("Hibás kapcsolati profil a fájlban.", ex); }

                profile.Name = profile.Name.Trim();
                if (!ids.Add(profile.Id) || !names.Add(profile.Name))
                    throw new JsonException("Ismétlődő név vagy azonosító a kapcsolati profilok között.");
            }

            return file.Profiles.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private async Task WriteAsync(List<DatabaseConnectionProfile> profiles)
        {
            string directory = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");
            try
            {
                var file = new ProfileFile { FormatVersion = 1, Profiles = profiles };
                await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(file, JsonOptions));
                File.Move(temporaryPath, _path, overwrite: true);
            }
            finally
            {
                try { File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Validate(DatabaseConnectionProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            if (profile.Id == Guid.Empty)
                throw new ArgumentException("A kapcsolat azonosítója nem lehet üres.");
            ArgumentException.ThrowIfNullOrWhiteSpace(profile.Name);
            ArgumentNullException.ThrowIfNull(profile.Options);
            var options = profile.Options;
            if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.Database) ||
                string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Schema))
                throw new ArgumentException("A szerver, az adatbázis, a felhasználónév és a séma megadása kötelező.");
            if (options.Port is < 1 or > 65535)
                throw new ArgumentException("A port értéke 1 és 65535 között lehet.");
            if (!Enum.IsDefined(options.TlsMode))
                throw new ArgumentException("Érvénytelen TLS-beállítás.");
        }

        private sealed class ProfileFile
        {
            public required int FormatVersion { get; set; }
            public required List<DatabaseConnectionProfile> Profiles { get; set; }
        }
    }
}
