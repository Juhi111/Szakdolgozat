namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Egy elemzési szabály találatát tárolja az érintett utasítás sorszámával,
    /// a szabály azonosítójával, a leírással és a javaslattal.
    /// A forráspozíció segíti az érintett SQL-rész kijelölését a felületen.
    /// </summary>
    public class SqlAnalysisFinding
    {
        public int StatementIndex { get; set; }
        public string RuleId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public int? SourceOffset { get; set; }
    }
}
