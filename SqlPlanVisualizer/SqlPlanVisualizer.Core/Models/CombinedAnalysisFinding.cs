namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// SQL-szerkezeti adat és tervmegfigyelés összekapcsolásából származó megállapítás.
    /// </summary>
    public class CombinedAnalysisFinding
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public string Brief => SqlPlanVisualizer.Core.Analysis.FindingTextCatalog.Get(RuleId, Description, Recommendation).Summary;
        [System.Text.Json.Serialization.JsonIgnore]
        public string NextStep => SqlPlanVisualizer.Core.Analysis.FindingTextCatalog.Get(RuleId, Description, Recommendation).NextStep;

        public string? RelatedSqlRuleId { get; init; }
        public required string RuleId { get; init; }
        public required string Title { get; init; }
        public required string SqlObservation { get; init; }
        public required string PlanObservation { get; init; }
        public required string Description { get; init; }
        public required string Recommendation { get; init; }

        public required PlanAnalysisFinding RelatedPlanFinding { get; init; }
    }
}