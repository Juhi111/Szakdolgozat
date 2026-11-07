using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SqlPlanVisualizer.Core.Models;
using System;

namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    public class PdfService
    {

        public PdfService() {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [Obsolete]
        public void SaveResultToPdf(CombinedAnalysisResult result, string path)
        {
        
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Content().Column(column =>
                    {
                        column.Item().Text("Összesített elemzés");

                        column.Item().Text($"PlanningTime: {result.PlanningTime}");
                        column.Item().Text($"Execution Time: {result.ExecutionTime}");

                        foreach (var recommendation in result.CombinedFindings)
                        {
                            column.Item().Text(recommendation.Description);
                        }
                    });
                });
            })
            .GeneratePdf(path);
        }
    }
}
