using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SqlPlanVisualizer.Core.Models.History;

namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    /// <summary>Az elemzési előzmények fájlba mentését kezeli.</summary>
    public class AnalysisHistoryService
    {
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Külön JSON-fájlba menti a bejegyzést, és visszaadja a fájl elérési útját.
        /// </summary>
        public async Task<string> SaveAsync(AnalysisHistoryEntry entry, string directory)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);

            if (!Path.IsPathFullyQualified(directory))
                throw new ArgumentException("A mentési mappa teljes elérési útját kell megadni.", nameof(directory));

            Directory.CreateDirectory(directory);

            string timestamp = entry.CreatedAt.UtcDateTime.ToString("yyyyMMdd_HHmmss_fff");
            string fileName = $"{timestamp}_{entry.Id:N}.json";
            string destination = Path.Combine(directory, fileName);
            string temporaryPath = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");

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
                    await JsonSerializer.SerializeAsync(stream, entry, jsonOptions);
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

        /// <summary>Egy mentett elemzési bejegyzést olvas vissza.</summary>
        public async Task<AnalysisHistoryEntry> LoadAsync(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            string json = await File.ReadAllTextAsync(path);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Az alapértékek miatt a hiányzó mezőket még deszerializálás előtt ellenőrizzük.
            if ( root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("FormatVersion", out var version) ||
                 version.ValueKind != JsonValueKind.Number ||
                !version.TryGetInt32(out int formatVersion) ||                
                !root.TryGetProperty("Id", out _) ||
                !root.TryGetProperty("CreatedAt", out _) ||
                !HasArray(root, "SqlFindings") ||
                !HasArray(root, "PlanFindings") ||
                !HasArray(root, "CombinedFindings"))
            {
                throw new JsonException("A fájl nem érvényes elemzési előzmény.");
            }

            if (formatVersion != 1)
                throw new JsonException($"Nem támogatott előzményformátum: {formatVersion}.");

            var entry = JsonSerializer.Deserialize<AnalysisHistoryEntry>(json, jsonOptions)
                ?? throw new JsonException("Az előzmény nem tartalmaz bejegyzést.");

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
                throw new JsonException("Az előzmény hiányos vagy hibás adatokat tartalmaz.");
            }

            return entry;
        }

        private static bool HasArray(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array;
        }

        /// <summary>
        /// Betölti a mappa előzményeit, a legújabbal kezdve.
        /// A hibás fájlokról külön hibalistát ad vissza.
        /// </summary>
        public async Task<(List<AnalysisHistoryEntry> Entries, List<string> Errors)> LoadAllAsync(string directory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);

            if (!Path.IsPathFullyQualified(directory))
                throw new ArgumentException("Teljes mappaútvonal szükséges.", nameof(directory));

            var entries = new List<AnalysisHistoryEntry>();
            var errors = new List<string>();

            string[] paths;

            try
            {
                paths = Directory.GetFiles(directory, "*.json");
            }
            catch (DirectoryNotFoundException)
            {
                return (entries, errors);
            }

            foreach (string path in paths)
            {
                try
                {
                    entries.Add(await LoadAsync(path));
                }
                catch (Exception ex) when (
                    ex is IOException or UnauthorizedAccessException or JsonException)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
                }
            }

            return (entries.OrderByDescending(entry => entry.CreatedAt).ToList(), errors);
        }
    }
}