namespace SqlPlanVisualizer.Core.Models.Database
{
    /// <summary>Névvel menthető kapcsolati beállítások, jelszó nélkül.</summary>
    public sealed class DatabaseConnectionProfile
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required DatabaseConnectionOptions Options { get; set; }
    }
}
