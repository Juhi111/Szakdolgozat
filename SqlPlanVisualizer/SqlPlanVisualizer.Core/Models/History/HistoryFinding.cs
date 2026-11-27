namespace SqlPlanVisualizer.Core.Models.History
{
    /// <summary>Egy elemzési találat visszanézéshez szükséges adatai.</summary>
    public class HistoryFinding
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public string Brief => SqlPlanVisualizer.Core.Analysis.FindingTextCatalog.Get(RuleId, Description, Recommendation).Summary;
        [System.Text.Json.Serialization.JsonIgnore]
        public string NextStep => SqlPlanVisualizer.Core.Analysis.FindingTextCatalog.Get(RuleId, Description, Recommendation).NextStep;

        // Optional fields keep older history files readable.
        public string? EvidenceKey { get; set; }
        public string? RelatedPlanKey { get; set; }
        public string? RelatedSqlRuleId { get; set; }
        public string? NodePath { get; set; }
        public string RuleId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Recommendation { get; set; } = string.Empty;

        public string? NodeType { get; set; }

        public string? RelationName { get; set; }

        public string? SqlObservation { get; set; }

        public string? PlanObservation { get; set; }
    }
}