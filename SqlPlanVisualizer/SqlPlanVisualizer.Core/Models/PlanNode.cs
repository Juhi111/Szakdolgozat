using System.Text.Json;

namespace SqlPlanVisualizer.Core.Models
{
    public class PlanNode
    {
        public string NodeType { get; set; } = string.Empty;
        public string? RelationName { get; set; }
        public List<PlanNode> Children { get; set; } = new();
        public double? PlanRows { get; set; }
        public double? ActualRows { get; set; }
        public double? StartupCost { get; set; }
        public double? TotalCost { get; set; }
        public double? ActualTotalTime { get; set; }
        public double? ActualLoops { get; set; }    
        /// <summary>A Filter támogatott részkifejezéseinek AST-azonosítói.</summary>
        public HashSet<string> FilterExpressionSignatures { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, JsonElement> AdditionalProperties { get; } = new();
    }
}
