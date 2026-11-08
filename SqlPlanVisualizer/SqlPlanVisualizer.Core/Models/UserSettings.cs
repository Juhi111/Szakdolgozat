namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>A felhasználó által megadható alkalmazásbeállítások.</summary>
    public class UserSettings
    {
        public bool SaveAnalysisHistory { get; set; }

        public bool IncludeSourceFiles { get; set; }

        public string HistoryDirectory { get; set; } = string.Empty;
    }
}