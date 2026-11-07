namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Egy összepárosított SQL-utasítás és végrehajtási terv elemzési eredményeit tárolja.
    /// </summary>
    public class CombinedAnalysisResult
    {
        /// <summary>
        /// Az SQL és a terv összekapcsolásából származó megállapítások.
        /// </summary>
        public List<CombinedAnalysisFinding> CombinedFindings { get; } = new();

        public CombinedAnalysisResult(
            StatementPlanPair pair,
            IEnumerable<SqlAnalysisFinding> sqlFindings,
            IEnumerable<PlanAnalysisFinding> planFindings)
        {
            ArgumentNullException.ThrowIfNull(pair);
            ArgumentNullException.ThrowIfNull(sqlFindings);
            ArgumentNullException.ThrowIfNull(planFindings);

            Pair = pair;
            SqlFindings = sqlFindings.ToList().AsReadOnly();
            PlanFindings = planFindings.ToList().AsReadOnly();
        }

        public StatementPlanPair Pair { get; }
        public IReadOnlyList<SqlAnalysisFinding> SqlFindings { get; }
        public IReadOnlyList<PlanAnalysisFinding> PlanFindings { get; }

        public double? PlanningTime => Pair.Plan.PlanningTime;
        public double? ExecutionTime => Pair.Plan.ExecutionTime;
    }
}