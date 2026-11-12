using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using SqlPlanVisualizer.Core.Models.History;

namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    /// <summary>
    /// Az elemzési előzmények mentését, betöltését és törlését kezeli.
    /// </summary>
    public class AnalysisHistoryService
    {
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Külön JSON-fájlba menti a bejegyzést,
        /// és visszaadja a fájl elérési útját.
        /// </summary>
        public async Task<string> SaveAsync(
            AnalysisHistoryEntry entry,
            string directory)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ValidateDirectory(directory);

            Directory.CreateDirectory(directory);

            string timestamp =
                entry.CreatedAt.UtcDateTime.ToString("yyyyMMdd_HHmmss_fff");

            string fileName = $"{timestamp}_{entry.Id:N}.json";
            string destination = Path.Combine(directory, fileName);

            string temporaryPath = Path.Combine(
                directory,
                $"{Guid.NewGuid():N}.tmp");

            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        entry,
                        jsonOptions);
                }

                File.Move(temporaryPath, destination);

                return destination;
            }
            finally
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>
        /// Egy mentett elemzési bejegyzést olvas vissza,
        /// és ellenőrzi a kötelező adatait.
        /// </summary>
        public async Task<AnalysisHistoryEntry> LoadAsync(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            string json = await File.ReadAllTextAsync(path);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // A hiányzó mezőket még deszerializálás előtt ellenőrizzük.
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("FormatVersion", out var version) ||
                version.ValueKind != JsonValueKind.Number ||
                !version.TryGetInt32(out int formatVersion) ||
                !root.TryGetProperty("Id", out _) ||
                !root.TryGetProperty("CreatedAt", out _) ||
                !HasArray(root, "SqlFindings") ||
                !HasArray(root, "PlanFindings") ||
                !HasArray(root, "CombinedFindings"))
            {
                throw new JsonException(
                    "A fájl nem érvényes elemzési előzmény.");
            }

            if (formatVersion != 1)
            {
                throw new JsonException(
                    $"Nem támogatott előzményformátum: {formatVersion}.");
            }

            var entry = JsonSerializer.Deserialize<AnalysisHistoryEntry>(
                json,
                jsonOptions)
                ?? throw new JsonException(
                    "Az előzmény nem tartalmaz bejegyzést.");

            if (entry.Id == Guid.Empty ||
                entry.CreatedAt == default ||
                entry.StatementIndex < 1 ||
                entry.PlanIndex < 1 ||
                entry.SqlFindings is null ||
                entry.PlanFindings is null ||
                entry.CombinedFindings is null ||
                entry.SqlFindings.Any(finding => finding is null) ||
                entry.PlanFindings.Any(finding => finding is null) ||
                entry.CombinedFindings.Any(finding => finding is null))
            {
                throw new JsonException(
                    "Az előzmény hiányos vagy hibás adatokat tartalmaz.");
            }

            return entry;
        }

        /// <summary>
        /// Betölti a mappa előzményeit, a legújabbal kezdve.
        /// A hibás fájlokról külön hibalistát ad vissza.
        /// </summary>
        public async Task<(
            List<AnalysisHistoryEntry> Entries,
            List<string> Errors)> LoadAllAsync(string directory)
        {
            var entries = new List<AnalysisHistoryEntry>();
            var errors = new List<string>();

            foreach (string path in GetHistoryPaths(directory))
            {
                try
                {
                    entries.Add(await LoadAsync(path));
                }
                catch (Exception ex) when (
                    ex is IOException or
                    UnauthorizedAccessException or
                    JsonException)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
                }
            }

            return (
                entries.OrderByDescending(entry => entry.CreatedAt).ToList(),
                errors);
        }

        /// <summary>
        /// Az azonosító alapján megkeresi és törli az előzmény fájlját.
        /// Ha nem található, false értékkel tér vissza.
        /// </summary>
        public async Task<bool> DeleteAsync(
            Guid entryId,
            string directory)
        {
            if (entryId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Az előzmény azonosítója nem lehet üres.",
                    nameof(entryId));
            }

            foreach (string path in GetHistoryPaths(directory))
            {
                AnalysisHistoryEntry entry;

                try
                {
                    entry = await LoadAsync(path);
                }
                catch (JsonException)
                {
                    // Más JSON-fájlt vagy hibás előzményt nem törlünk.
                    continue;
                }

                if (entry.Id != entryId)
                    continue;

                File.Delete(path);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Törli a mappában található érvényes előzményfájlokat.
        /// Visszaadja a törlések számát és a feldolgozási hibákat.
        /// Egy fájl hibája nem szakítja meg a többi törlését.
        /// </summary>
        public async Task<(
            int DeletedCount,
            List<string> Errors)> DeleteAllAsync(string directory)
        {
            int deletedCount = 0;
            var errors = new List<string>();

            foreach (string path in GetHistoryPaths(directory))
            {
                try
                {
                    // Csak ellenőrzött előzményfájlt törlünk.
                    await LoadAsync(path);

                    File.Delete(path);
                    deletedCount++;
                }
                catch (Exception ex) when (
                    ex is IOException or
                    UnauthorizedAccessException or
                    JsonException)
                {
                    errors.Add(
                        $"{Path.GetFileName(path)}: " +
                        $"Nem lett törölve. {ex.Message}");
                }
            }

            return (deletedCount, errors);
        }

        /// <summary>
        /// Visszaadja a mappa közvetlen JSON-fájljait.
        /// Nem létező mappa esetén üres tömböt ad vissza.
        /// </summary>
        private static string[] GetHistoryPaths(string directory)
        {
            ValidateDirectory(directory);

            try
            {
                return Directory.GetFiles(directory, "*.json");
            }
            catch (DirectoryNotFoundException)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Ellenőrzi, hogy teljes mappaútvonalat kaptunk-e.
        /// </summary>
        private static void ValidateDirectory(string directory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);

            if (!Path.IsPathFullyQualified(directory))
            {
                throw new ArgumentException(
                    "Teljes mappaútvonal szükséges.",
                    nameof(directory));
            }
        }

        /// <summary>
        /// Ellenőrzi, hogy a megadott JSON-mező tömb-e.
        /// </summary>
        private static bool HasArray(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.Array;
        }
    }
}