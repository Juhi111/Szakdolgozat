namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Egy lekérdezés közvetlen táblahivatkozását írja le.
    /// </summary>
    public class SqlRelationSummary
    {
        public string RelationName { get; init; } = string.Empty;
        public string? SchemaName { get; init; }
        public string? Alias { get; init; }
    }
}