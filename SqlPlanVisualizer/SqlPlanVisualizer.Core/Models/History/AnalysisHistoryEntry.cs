using System;
using System.Collections.Generic;

namespace SqlPlanVisualizer.Core.Models.History
{
    /// <summary>Egy párosított elemzés menthető pillanatképe.</summary>
    public class AnalysisHistoryEntry
    {
        public int FormatVersion { get; set; } = 1;

        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public int StatementIndex { get; set; }

        public int PlanIndex { get; set; }

        public double? PlanningTime { get; set; }

        public double? ExecutionTime { get; set; }

        public string? SqlText { get; set; }

        public string? ExplainJson { get; set; }

        public List<HistoryFinding> SqlFindings { get; set; } = new();

        public List<HistoryFinding> PlanFindings { get; set; } = new();

        public List<HistoryFinding> CombinedFindings { get; set; } = new();
    }
}