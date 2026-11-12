using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    /// <summary>A felhasználói beállításokat helyi JSON-fájlban tárolja.</summary>
    public class UserSettingsService
    {
        private readonly string settingsPath;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        public UserSettingsService() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SqlPlanVisualizer", "user-settings.json"))
        {
        }

        public UserSettingsService(string settingsPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
            if (!Path.IsPathFullyQualified(settingsPath))
                throw new ArgumentException("Teljes fájlútvonal szükséges.", nameof(settingsPath));

            this.settingsPath = settingsPath;
        }

        /// <summary>
        /// Biztonsági másolat után alapbeállításokra állítja vissza a konfigurációt.
        /// Sikertelen másolatkészítés esetén az eredeti fájlt nem írja felül.
        /// Az előzményfájlokat nem módosítja.
        /// </summary>
        public async Task<(UserSettings Settings, string? BackupPath)> ResetAsync()
        {
            string? backupPath = null;
            if (File.Exists(settingsPath))
            {
                backupPath = settingsPath + $".{Guid.NewGuid():N}.bak";
                File.Copy(settingsPath, backupPath, overwrite: false);
            }

            var settings = CreateDefaultSettings();
            await SaveAsync(settings);
            return (settings, backupPath);
        }

        /// <summary>
        /// Betölti a beállításokat. Első indításkor alapértékeket ad vissza.
        /// </summary>
        public async Task<UserSettings> LoadAsync()
        {
            if (!File.Exists(settingsPath))
                return CreateDefaultSettings();

            string json = await File.ReadAllTextAsync(settingsPath);

            var settings = JsonSerializer.Deserialize<UserSettings>(json)
                ?? throw new JsonException("A beállításfájl nem tartalmaz beállításokat.");

            if (string.IsNullOrWhiteSpace(settings.HistoryDirectory))
                settings.HistoryDirectory = CreateDefaultSettings().HistoryDirectory;

            return settings;
        }

        /// <summary>
        /// Elmenti a beállításokat; a célfájlt csak a sikeres írás után cseréli le.
        /// </summary>
        public async Task SaveAsync(UserSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            string directory = Path.GetDirectoryName(settingsPath)!;
            Directory.CreateDirectory(directory);

            string temporaryPath = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");

            try
            {
                string json = JsonSerializer.Serialize(settings, jsonOptions);
                await File.WriteAllTextAsync(temporaryPath, json);
                File.Move(temporaryPath, settingsPath, overwrite: true);
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

        private static UserSettings CreateDefaultSettings()
        {
            return new UserSettings
            {
                SaveAnalysisHistory = false,
                IncludeSourceFiles = false,
                HistoryDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SqlPlanVisualizer",
                    "History")
            };
        }
    }
}