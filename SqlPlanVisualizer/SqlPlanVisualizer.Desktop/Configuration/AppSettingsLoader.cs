using System.IO;
using System.Text.Json;

namespace SqlPlanVisualizer.Desktop.Configuration
{
    /// <summary>
    /// Az alkalmazás mellől beolvassa és ellenőrzi a konfigurációt.
    /// </summary>
    public static class AppSettingsLoader
    {
        /// <summary>
        /// Betölti az appsettings.json fájlt.
        /// Hiányzó vagy érvénytelen beállítás esetén kivételt dob.
        /// </summary>
        public static AppSettings Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            string json = File.ReadAllText(path);

            var settings = JsonSerializer.Deserialize<AppSettings>(json)
                ?? throw new JsonException("A konfiguráció nem lehet null.");

            var rowSettings = settings.PlanAnalysis?.RowEstimateMismatch;
            var sqlSettings = settings.SqlAnalysis;

            if (rowSettings is null || sqlSettings is null)
                throw new JsonException("Hiányzó konfigurációs szakasz.");

            if (!double.IsFinite(rowSettings.RatioThreshold) || rowSettings.RatioThreshold <= 1)
                throw new JsonException("A RatioThreshold értékének végesnek és 1-nél nagyobbnak kell lennie.");

            if (!double.IsFinite(rowSettings.DifferenceThreshold) || rowSettings.DifferenceThreshold <= 0)
                throw new JsonException("A DifferenceThreshold értékének végesnek és pozitívnak kell lennie.");

            if (sqlSettings.LargeOffsetThreshold <= 0)
                throw new JsonException("A LargeOffsetThreshold értékének pozitívnak kell lennie.");

            var filterSettings = settings.PlanAnalysis.HighFilterRemoval;

            if (filterSettings is null)
                throw new JsonException("Hiányzik a HighFilterRemoval konfiguráció.");

            if (!double.IsFinite(filterSettings.MinimumRemovedRows) || filterSettings.MinimumRemovedRows <= 0)
                throw new JsonException("A MinimumRemovedRows értékének pozitív, véges számnak kell lennie.");

            if (!double.IsFinite(filterSettings.RemovalRatioThreshold) ||
                filterSettings.RemovalRatioThreshold <= 0 ||
                filterSettings.RemovalRatioThreshold > 1)
            {
                throw new JsonException("A RemovalRatioThreshold értékének 0-nál nagyobbnak és legfeljebb 1-nek kell lennie.");
            }

            var heapSettings = settings.PlanAnalysis.HighHeapFetches;

            if (heapSettings is null)
                throw new JsonException("Hiányzik a HighHeapFetches konfiguráció.");

            if (!double.IsFinite(heapSettings.MinimumHeapFetches) || heapSettings.MinimumHeapFetches <= 0)
            {
                throw new JsonException("A MinimumHeapFetches értékének pozitív, véges számnak kell lennie.");
            }

            var repeatedScanSettings = settings.PlanAnalysis.RepeatedSequentialScan;

            if (repeatedScanSettings is null)
                throw new JsonException("Hiányzik a RepeatedSequentialScan konfiguráció.");

            if (!double.IsFinite(repeatedScanSettings.MinimumLoops) || repeatedScanSettings.MinimumLoops <= 1)
                throw new JsonException("A MinimumLoops értékének 1-nél nagyobb, véges számnak kell lennie.");

            if (!double.IsFinite(repeatedScanSettings.MinimumExaminedRows) ||
                repeatedScanSettings.MinimumExaminedRows <= 0)
            {
                throw new JsonException("A MinimumExaminedRows értékének pozitív, véges számnak kell lennie.");
            }

            var sortLimitSettings = settings.PlanAnalysis.LargeSortForLimit;

            if (sortLimitSettings is null)
                throw new JsonException("Hiányzik a LargeSortForLimit konfiguráció.");

            if (!double.IsFinite(sortLimitSettings.MinimumInputRows) || sortLimitSettings.MinimumInputRows <= 0)
                throw new JsonException("A MinimumInputRows értékének pozitív, véges számnak kell lennie.");

            if (!double.IsFinite(sortLimitSettings.InputOutputRatioThreshold) ||
                sortLimitSettings.InputOutputRatioThreshold <= 1)
            {
                throw new JsonException("Az InputOutputRatioThreshold értékének 1-nél nagyobb, véges számnak kell lennie.");
            }

            return settings;
        }
    }
}