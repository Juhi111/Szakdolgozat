using System.Text.Json.Serialization;
namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos
{
    // A PostgreSQL Explain JSON csomópontjainak reprezentációja
    public class PostgreSqlPlanNodeDto
    {
        [JsonPropertyName("Node Type")]
        public string? NodeType { get; set; }

        [JsonPropertyName("Relation Name")]
        public string? RelationName { get; set; }

        [JsonPropertyName("Plans")]
        public List<PostgreSqlPlanNodeDto> Plans { get; set; } = new();

        [JsonPropertyName("Plan Rows")]
        public double? PlanRows { get; set; }

        [JsonPropertyName("Actual Rows")]
        public double? ActualRows { get; set; }

        [JsonPropertyName("Startup Cost")]
        public double? StartupCost { get; set; }

        [JsonPropertyName("Total Cost")]
        public double? TotalCost { get; set; }

        [JsonPropertyName("Actual Total Time")]
        public double? ActualTotalTime { get; set; }

        [JsonPropertyName("Actual Loops")]
        public double? ActualLoops { get; set; }
    }
}
