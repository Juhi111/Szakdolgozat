using SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    /// <summary>
    /// A PostgreSQL-specifikus tervcsomópontokat a Core réteg PlanNode modelljére alakítja.
    /// </summary>
    public class PostgreSqlPlanMapper
    {
        /// <summary>
        /// Átmásolja a csomópont adatait egy új PlanNode objektumba,
        /// és rekurzívan átalakítja a gyermekeit, megőrizve a fa szerkezetét.
        /// </summary>
        /// <param name="dto">Az átalakítandó részfa gyökércsomópontja.</param>
        /// <returns>Az átalakított részfa gyökércsomópontja.</returns>
        /// <exception cref="ArgumentNullException">A csomópont vagy valamelyik leszármazottja null.</exception>
        /// <exception cref="ArgumentException">Valamelyik csomópont művelettípusa üres, vagy gyermeklistája null.</exception>
        public PlanNode MapNode(PostgreSqlPlanNodeDto dto) {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.NodeType))
                throw new ArgumentException("A csomópont művelettípusa nem lehet üres.", nameof(dto));

            PlanNode planNode = new();
            planNode.NodeType = dto.NodeType;
            planNode.RelationName = dto.RelationName;            
            planNode.PlanRows = dto.PlanRows;
            planNode.ActualRows = dto.ActualRows;
            planNode.StartupCost = dto.StartupCost;
            planNode.TotalCost = dto.TotalCost;
            planNode.ActualTotalTime = dto.ActualTotalTime;
            planNode.ActualLoops = dto.ActualLoops;
            foreach (var property in dto.AdditionalProperties)
            {
                planNode.AdditionalProperties.Add(property.Key, property.Value.Clone());
            }

            if (dto.Plans is null){
                throw new ArgumentException("A csomópont gyermeklistája nem lehet null.", nameof(dto));
            }

            foreach (var childDto in dto.Plans)
            { 
                var childNode = MapNode(childDto);
                planNode.Children.Add(childNode);
            }

            return planNode;
        }
    }
}
