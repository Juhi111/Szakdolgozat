using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    /// <summary>
    /// Egy tervcsomópont helyét és szülői környezetét tárolja az elemzés során.
    /// </summary>
    public class PlanAnalysisContext
    {
        public PlanAnalysisContext(PlanNode node, int planIndex, PlanAnalysisContext? parent = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (planIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(planIndex));

            Node = node;
            PlanIndex = planIndex;
            Parent = parent;
        }

        public PlanNode Node { get; }
        public int PlanIndex { get; }
        public PlanAnalysisContext? Parent { get; }
    }
}