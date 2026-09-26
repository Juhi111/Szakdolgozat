using System.Text.Json.Serialization;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos
{
    // A PostgreSQL Explain JSON teljes tervének reprezentációja
    public class PostgreSqlExplainDto
    {
        [JsonPropertyName("Plan")]
        public PostgreSqlPlanNodeDto? Plan { get; set; }
        [JsonPropertyName("Planning Time")]
        public double? PlanningTime { get; set; }
        [JsonPropertyName("Execution Time")]
        public double? ExecutionTime { get; set; }
    }
}
