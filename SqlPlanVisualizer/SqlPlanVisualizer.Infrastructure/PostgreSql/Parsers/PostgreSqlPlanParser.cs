using SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos;
using System.Text.Json;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers
{
    /// <summary>
    /// A PostgreSQL EXPLAIN JSON-szövegét DTO-objektumokká alakítja,
    /// és ellenőrzi a tervek alapvető szerkezeti épségét.
    /// </summary>
    public class PostgreSqlPlanParser
    {
        /// <summary>
        /// Deszerializálja a terveket, majd ellenőrzi a gyökércsomópontokat
        /// és azok összes leszármazottját.
        /// </summary>
        /// <param name="json">Az EXPLAIN eredményét tartalmazó JSON-tömb szövege.</param>
        /// <returns>A feldolgozott és ellenőrzött tervek listája.</returns>
        /// <exception cref="ArgumentException">A szöveg null, üres vagy csak whitespace karaktereket tartalmaz.</exception>
        /// <exception cref="JsonException">A JSON nem megfelelő formátumú, a tervlista üres, vagy valamelyik terv szerkezetileg hiányos.</exception>
        public List<PostgreSqlExplainDto> Parse(string json) {

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("A JSON-szöveg nem tartalmaz adatot.", nameof(json));
            }
            
            List<PostgreSqlExplainDto>? jsonData = JsonSerializer.Deserialize<List<PostgreSqlExplainDto>>(json);

            if (jsonData is null)
                throw new JsonException("Sikertelen deserialize");
            if(jsonData.Count == 0)
                throw new JsonException("Üres az explain");

            foreach (var item in jsonData) { 
                if(item is null || item.Plan is null)
                    throw new JsonException("Sikertelen deserialize");
                ValidateNode(item.Plan);
            }

            return jsonData;
        }

        /// <summary>
        /// Rekurzívan ellenőrzi, hogy a csomópontok nem null értékűek,
        /// rendelkeznek művelettípussal, és a gyermeklistájuk nem null.
        /// </summary>
        /// <param name="node">Az ellenőrizendő részfa gyökércsomópontja.</param>
        /// <exception cref="JsonException">A csomópont vagy valamelyik leszármazottja nem teljesíti a feltételeket.</exception>
        private void ValidateNode(PostgreSqlPlanNodeDto? node) {

            if (node is null)
                throw new JsonException("A terv null csomópontot tartalmaz.");
            else if (string.IsNullOrWhiteSpace(node.NodeType))
                throw new JsonException("A node nem tartalmaz node type-ot");
            else if (node.Plans is null)
                throw new JsonException("A csomópont Plans listája null.");
            foreach (var plan in node.Plans) { 
                ValidateNode(plan);
            }
        }
    }
}
