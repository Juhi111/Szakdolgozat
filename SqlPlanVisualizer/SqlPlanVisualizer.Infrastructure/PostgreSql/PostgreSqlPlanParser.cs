using SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos;
using System.Text.Json;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    // A PostgreSQL Explain JSON szövegének feldolgozásáért felelős osztály
    public class PostgreSqlPlanParser
    {
        // A Parse metódus feldolgozza a PostgreSQL Explain JSON szöveget, és visszaadja a deszerializált objektumokat
        public List<PostgreSqlExplainDto> Parse(string json) {

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("A JSON-szöveg nem tartalmaz adatot.", nameof(json));
            }
            
            List<PostgreSqlExplainDto>? jsonData = JsonSerializer.Deserialize<List<PostgreSqlExplainDto>>(json);

            if (jsonData == null)
                throw new JsonException("Sikertelen deserialize");
            if(jsonData.Count == 0)
                throw new JsonException("Üres az explain");

            foreach (var item in jsonData) { 
                if(item == null || item.Plan == null)
                    throw new JsonException("Sikertelen deserialize");
                ValidateNode(item.Plan);
            }

            return jsonData;
        }

        // A ValidateNode metódus rekurzívan ellenőrzi a csomópontokat, hogy biztosítsa, hogy minden szükséges mező jelen van
        private void ValidateNode(PostgreSqlPlanNodeDto? node) {

            if (node == null)
                throw new JsonException("A terv null csomópontot tartalmaz.");
            else if (string.IsNullOrWhiteSpace(node.NodeType))
                throw new JsonException("A node nem tartalmaz node type-ot");
            else if (node.Plans == null)
                throw new JsonException("A csomópont Plans listája null.");
            foreach (var plan in node.Plans) { 
                ValidateNode(plan);
            }
        }
    }
}
