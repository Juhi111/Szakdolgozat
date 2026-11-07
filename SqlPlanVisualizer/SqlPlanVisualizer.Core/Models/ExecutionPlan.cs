namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Egy végrehajtási tervet és annak teljes tervezési, illetve végrehajtási idejét tárolja.
    /// </summary>
    public class ExecutionPlan
    {
        public ExecutionPlan(int planIndex, PlanNode root)
        {
            if (planIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(planIndex));

            ArgumentNullException.ThrowIfNull(root);

            PlanIndex = planIndex;
            Root = root;
        }

        public int PlanIndex { get; }
        public PlanNode Root { get; }

        // A tervhez tartozó időadatok ezredmásodpercben, ha szerepelnek a JSON-ban.
        public double? PlanningTime { get; init; }
        public double? ExecutionTime { get; init; }
    }
}