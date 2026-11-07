using System.Text.Json;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules
{
    /// <summary>
    /// SQL-táblahivatkozások és tervszűrések egyezését ellenőrzi.
    /// </summary>
    internal static class PlanFilterMatcher
    {
        public static bool MatchesRelation(PlanNode node, SqlRelationSummary relation)
        {
            if (node.RelationName != relation.RelationName)
                return false;

            if (ReadText(node, "Alias") != (relation.Alias ?? relation.RelationName))
                return false;

            return relation.SchemaName is null ||
                ReadText(node, "Schema") == relation.SchemaName;
        }

        public static string? ReadText(PlanNode node, string key)
        {
            return node.AdditionalProperties.TryGetValue(key, out var value) &&
                value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

    }
}
