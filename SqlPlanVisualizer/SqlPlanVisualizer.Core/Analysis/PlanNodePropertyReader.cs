using System.Text.Json;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    /// <summary>
    /// Típusellenőrzéssel olvassa ki a tervcsomópont további adatait.
    /// </summary>
    public static class PlanNodePropertyReader
    {
        /// <summary>
        /// Kiolvassa a megadott szöveges mezőt.
        /// </summary>
        /// <param name="node">A vizsgált tervcsomópont.</param>
        /// <param name="propertyName">Az eredeti JSON-mező neve.</param>
        /// <param name="value">A kiolvasott szöveg, sikertelenség esetén üres szöveg.</param>
        /// <returns>Igaz, ha a mező létezik és szöveges értékű.</returns>
        public static bool TryReadString(PlanNode node, string propertyName, out string value)
        {
            ArgumentNullException.ThrowIfNull(node);

            value = string.Empty;

            if (!node.AdditionalProperties.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
                return false;

            value = property.GetString()!;
            return true;
        }

        /// <summary>
        /// Kiolvassa a megadott numerikus mezőt véges double értékként.
        /// </summary>
        /// <param name="node">A vizsgált tervcsomópont.</param>
        /// <param name="propertyName">Az eredeti JSON-mező neve.</param>
        /// <param name="value">A kiolvasott szám, sikertelenség esetén nulla.</param>
        /// <returns>Igaz, ha a mező véges számként kiolvasható.</returns>
        public static bool TryReadDouble(PlanNode node, string propertyName, out double value)
        {
            ArgumentNullException.ThrowIfNull(node);

            value = 0;

            if (!node.AdditionalProperties.TryGetValue(propertyName, out var property) ||
                property.ValueKind != JsonValueKind.Number ||
                !property.TryGetDouble(out double number) ||
                !double.IsFinite(number))
                return false;

            value = number;
            return true;
        }

        /// <summary>
        /// Visszaadja a csomóponthoz tartozó dolgozók JSON-objektumait.
        /// </summary>
        public static IEnumerable<JsonElement> GetWorkers(PlanNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (!node.AdditionalProperties.TryGetValue("Workers", out var workers) ||
                workers.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var worker in workers.EnumerateArray())
            {
                if (worker.ValueKind == JsonValueKind.Object)
                    yield return worker;
            }
        }

        /// <summary>
        /// Szöveges mezőt olvas ki egy JSON-objektumból.
        /// </summary>
        public static bool TryReadString(JsonElement source, string propertyName, out string value)
        {
            value = string.Empty;

            if (source.ValueKind != JsonValueKind.Object ||
                !source.TryGetProperty(propertyName, out var property) ||
                property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString()!;
            return true;
        }

        /// <summary>
        /// Véges számértéket olvas ki egy JSON-objektumból.
        /// </summary>
        public static bool TryReadDouble(JsonElement source, string propertyName, out double value)
        {
            value = 0;

            if (source.ValueKind != JsonValueKind.Object ||
                !source.TryGetProperty(propertyName, out var property) ||
                property.ValueKind != JsonValueKind.Number ||
                !property.TryGetDouble(out double number) ||
                !double.IsFinite(number))
            {
                return false;
            }

            value = number;
            return true;
        }

        /// <summary>
        /// Visszaadja a dolgozó azonosítóját tartalmazó megnevezést.
        /// </summary>
        public static string GetWorkerLabel(JsonElement worker)
        {
            if (worker.ValueKind == JsonValueKind.Object &&
                worker.TryGetProperty("Worker Number", out var number) &&
                number.ValueKind == JsonValueKind.Number &&
                number.TryGetInt32(out int workerNumber) &&
                workerNumber >= 0)
            {
                return $"Worker {workerNumber}";
            }

            return "Ismeretlen azonosítójú dolgozó";
        }
    }
}