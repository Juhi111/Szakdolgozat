using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;
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

        public AnalysisSummaryViewModel(CombinedAnalysisResult result, string? analyzedSql = null)
        {
            ArgumentNullException.ThrowIfNull(result);
            Result = result;
            if (analyzedSql is not null)
            {
                try
                {
                    var rewrites = new SqlRewriteService(new ISqlRewriteRule[] { new ArithmeticEqualityRewriteRule() });
                    RewriteSuggestions = rewrites.SuggestStatement(analyzedSql, result.Pair.Statement);
                }
                catch (FormatException)
                {
                    RewriteStatus = "Az SQL átírási javaslatai nem készültek el: a forrás nem dolgozható fel.";
                }
            }
            SaveResultToPdf = new AsyncRelayCommand(SaveToPdf);
        }

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
            return pdfExportService.ExportAnalysisSummaryToPdfAsync(Result);
        }
    }
}
