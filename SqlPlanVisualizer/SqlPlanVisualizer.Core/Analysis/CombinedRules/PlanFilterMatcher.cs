using System.Text.Json;
using System.Text.RegularExpressions;
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

        public static bool MatchesQualifier(string? qualifier, SqlRelationSummary relation)
        {
            if (qualifier is null)
                return true;

            if (relation.Alias is not null)
                return qualifier == relation.Alias;

            return qualifier == relation.RelationName ||
                (relation.SchemaName is not null &&
                 qualifier == $"{relation.SchemaName}.{relation.RelationName}");
        }

        public static bool ContainsFunction(string filter, string function, string column, SqlRelationSummary relation)
        {
            string columnPattern = IdentifierPattern(column);
            string qualifierPattern = IdentifierPattern(relation.Alias ?? relation.RelationName);

            string pattern = $@"(?<![\w$.""])(?:pg_catalog\.)?{Regex.Escape(function)}\s*\(\s*" +
                $@"(?:{qualifierPattern}\s*\.\s*)?{columnPattern}\s*\)";

            return Regex.IsMatch(filter, pattern, RegexOptions.CultureInvariant);
        }

        public static string? ReadText(PlanNode node, string key)
        {
            return node.AdditionalProperties.TryGetValue(key, out var value) &&
                value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string IdentifierPattern(string identifier)
        {
            string quoted = Regex.Escape("\"" + identifier.Replace("\"", "\"\"") + "\"");

            if (Regex.IsMatch(identifier, @"^[a-z_][a-z0-9_$]*$", RegexOptions.CultureInvariant))
                return $"(?:{Regex.Escape(identifier)}|{quoted})";

            return quoted;
        }
    }
}