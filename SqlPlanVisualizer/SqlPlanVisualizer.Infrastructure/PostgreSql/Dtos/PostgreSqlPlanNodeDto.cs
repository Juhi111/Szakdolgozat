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
        
    }
}
