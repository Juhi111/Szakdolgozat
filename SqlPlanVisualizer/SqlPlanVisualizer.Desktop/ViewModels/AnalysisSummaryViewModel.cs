using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using SqlPlanVisualizer.Desktop.Commands;
using SqlPlanVisualizer.Desktop.Services;
using System.Windows.Input;

namespace SqlPlanVisualizer.Desktop.ViewModels
{
    /// <summary>
    /// Egy elkészült összegzés adatait biztosítja az eredményablaknak.
    /// </summary>
    public class AnalysisSummaryViewModel
    {

        private readonly PdfExportService pdfExportService = new();

        public ICommand SaveResultToPdf { get; }

        public AnalysisSummaryViewModel(CombinedAnalysisResult result, string? analyzedSql = null, DatabaseSession? session = null, SqlRewriteComparisonService? comparisonService = null)
        {
            ArgumentNullException.ThrowIfNull(result);
            Result = result;
            if (analyzedSql is not null)
            {
                try
                {
                    var rewrites = new SqlRewriteService(SqlRewriteRuleFactory.CreateDefault());
                    RewriteSuggestions = rewrites.SuggestStatement(analyzedSql, result.Pair.Statement);
                }
                catch (FormatException)
                {
                    RewriteStatus = "Az SQL átírási javaslatai nem készültek el: a forrás nem dolgozható fel.";
                }
            }
            if (session is not null && comparisonService is not null)
                RewriteComparison = new RewriteComparisonViewModel(result, RewriteSuggestions, session, comparisonService);
            SaveResultToPdf = new AsyncRelayCommand(SaveToPdf);
        }

        public RewriteComparisonViewModel? RewriteComparison { get; }
        public CombinedAnalysisResult Result { get; }
        public IReadOnlyList<SqlRewriteSuggestion> RewriteSuggestions { get; } = Array.Empty<SqlRewriteSuggestion>();
        public string RewriteStatus { get; } = "Ehhez az utasításhoz jelenleg nincs támogatott átírási javaslat.";
        public string RewriteHeader => $"SQL-javítási javaslatok ({RewriteSuggestions.Count})";

        public string PairDescription =>
            $"{Result.Pair.Statement.StatementIndex}. SQL-utasítás – " +
            $"{Result.Pair.Plan.PlanIndex}. végrehajtási terv";

        public string PlanOriginText => Result.Pair.Plan.Origin?.Description ?? "A terv eredete nem ismert.";

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

        private Task SaveToPdf()
        {
            return pdfExportService.ExportAnalysisSummaryToPdfAsync(Result, RewriteComparison?.Comparison);
        }
    }
}
