using System.Text.Json.Serialization;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos
{
    /// <summary>
    /// A PostgreSQL EXPLAIN JSON egy tervét és annak összesített időadatait tárolja.
    /// A JSON-mezőneveket attribútumok rendelik a tulajdonságokhoz.
    /// </summary>
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
