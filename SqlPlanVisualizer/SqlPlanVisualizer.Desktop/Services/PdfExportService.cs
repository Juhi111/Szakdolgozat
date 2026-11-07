using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.FileSystem;

namespace SqlPlanVisualizer.Desktop.Services

{
    public class PdfExportService
    {
        public PdfExportService(CombinedAnalysisResult result, string filePath) {
            _filePath = filePath;
            _result = result;
        }

        [Obsolete]
        public void ExportAnalysisSummaryToPdf()
        {
            var pdfService = new PdfService();
            pdfService.SaveResultToPdf(_result, _filePath);
        }

        CombinedAnalysisResult _result;
        string _filePath;
    }
}
