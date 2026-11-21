namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>Egy külön ellenőrizendő SQL-változat; nem igazolja az eredmények azonosságát.</summary>
    public sealed record SqlRewriteSuggestion
    {
        public required string RuleId { get; init; }
        public required string Title { get; init; }
        public required string OriginalSql { get; init; }
        public required string SuggestedSql { get; init; }
        public required string Explanation { get; init; }
        public required string Preconditions { get; init; }
        public string VerificationStatus => "Ellenőrizendő javaslat – az eredményazonosság és a gyorsulás még nincs igazolva.";
    }
}
