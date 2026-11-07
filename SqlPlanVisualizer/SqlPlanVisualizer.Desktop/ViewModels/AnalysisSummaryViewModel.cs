using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    /// <summary>
    /// Egy elkészült összegzés adatait biztosítja az eredményablaknak.
    /// </summary>
    public class AnalysisSummaryViewModel
    {
        public AnalysisSummaryViewModel(CombinedAnalysisResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            Result = result;
        }

        public CombinedAnalysisResult Result { get; }

        public string PairDescription =>
            $"{Result.Pair.Statement.StatementIndex}. SQL-utasítás – " +
            $"{Result.Pair.Plan.PlanIndex}. végrehajtási terv";

        public string PlanningTimeText => Result.PlanningTime is double time
            ? $"{time:N2} ms"
            : "Nincs adat";

        public string ExecutionTimeText => Result.ExecutionTime is double time
            ? $"{time:N2} ms"
            : "Nincs adat";

        public string SqlFindingsHeader =>
            $"SQL-találatok ({Result.SqlFindings.Count})";

        public string PlanFindingsHeader =>
            $"Tervtalálatok ({Result.PlanFindings.Count})";
    }
}