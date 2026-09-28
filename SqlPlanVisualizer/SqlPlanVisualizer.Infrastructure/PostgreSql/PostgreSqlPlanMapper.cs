using SqlPlanVisualizer.Infrastructure.PostgreSql.Dtos;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class PostgreSqlPlanMapper
    {
        public PlanNode MapNode(PostgreSqlPlanNodeDto dto) {
            if (dto == null)
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

            if(dto.Plans == null){
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
