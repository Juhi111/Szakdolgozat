namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Egy végrehajtási terv elemzésekor keletkező találatot
    /// és az érintett tervcsomópontot tárolja.
    /// </summary>
    public class PlanAnalysisFinding
    {
        public int PlanIndex { get; set; }
        public PlanNode Node { get; }

        public string RuleId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;

        /// <summary>
        /// Létrehozza a találatot az érintett tervcsomóponthoz kapcsolva.
        /// </summary>
        /// <param name="node">A találathoz tartozó tervcsomópont.</param>
        /// <exception cref="ArgumentNullException">A csomópont null.</exception>
        public PlanAnalysisFinding(PlanNode node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }
    }
}