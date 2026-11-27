using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models.History;

namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    /// <summary>A párosított elemzés teljes, többoldalas PDF-összegzését készíti el.</summary>
    public partial class PdfService
    {
        private static readonly CultureInfo HungarianCulture = CultureInfo.GetCultureInfo("hu-HU");

        static PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Először ideiglenes fájlba generál, és csak siker esetén cseréli a célfájlt.
        /// A mentési és dokumentumgenerálási hibákat a hívó kezeli.
        /// </summary>
        public void SaveHistoryToPdf(AnalysisHistoryEntry result, string path)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            SaveDocument(CreateDocument(result), path);
        }

        private static void SaveDocument(IDocument document, string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string destination = Path.GetFullPath(path);
            string temporaryPath = Path.Combine(Path.GetDirectoryName(destination)!, $".sqlplan-{Guid.NewGuid():N}.tmp");

            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    document.GeneratePdf(stream);
                }

                File.Move(temporaryPath, destination, overwrite: true);
            }
            finally
            {
                // Egy takarítási hiba ne fedje el az eredeti mentési hibát.
                try { File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>A friss elemzés eredményét PDF-be menti.</summary>
        public void SaveResultToPdf(CombinedAnalysisResult result, string path, SqlRewriteComparison? comparison = null)
        {
            ArgumentNullException.ThrowIfNull(result);

            var entry = new AnalysisHistoryMapper().Map(
                result,
                includeSourceFiles: false,
                sqlText: null,
                explainJson: null);

            if (comparison is not null && !ReferenceEquals(comparison.Original, result))
                throw new ArgumentException("Az összehasonlítás másik eredeti elemzéshez tartozik.", nameof(comparison));
            SaveDocument(CreateDocument(entry, comparison), path);
        }

        private static IDocument CreateDocument(AnalysisHistoryEntry result, SqlRewriteComparison? comparison = null)
        {
            return Document.Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken3));
                page.Header().PaddingBottom(12).Text("SqlPlanVisualizer").FontSize(10).FontColor(Colors.Grey.Darken1);
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text("Elemzési összegzés").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken3);
                    column.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(info =>
                    {
                        info.Spacing(5);
                        info.Item()
    .Text($"{result.StatementIndex}. SQL-utasítás - {result.PlanIndex}. végrehajtási terv").SemiBold();
                        if (result.PlanOrigin is { } origin)
                            info.Item().Text(origin.Description);
                        info.Item().Text($"Tervezési idő: {FormatTime(result.PlanningTime)}");
                        info.Item().Text($"Végrehajtási idő: {FormatTime(result.ExecutionTime)}");
                    });

                    SectionTitle(column, "Mit érdemes átnézned?");
                    column.Item().Text(AnalysisOverview.Describe(result));
                    foreach (var item in AnalysisOverview.Create(result))
                        Card(column, item.Title, body =>
                        {
                            Field(body, "Röviden", item.Brief);
                            Field(body, "Következő lépés", item.NextStep);
                            foreach (var evidence in item.Evidence)
                            {
                                Field(body, evidence.Label, evidence.Description);
                            }
                            foreach (var recommendation in item.DetailRecommendations)
                                Field(body, "Részletes javaslat és feltételek", recommendation);
                        });
                    ComposeIndexAssessment(column, result.IndexAssessment);

                    ComposeMetadataEvidence(column, result.IndexAssessment);
                    if (comparison is not null)
                    {
                        column.Item().PageBreak();
                        ComposeComparison(column, comparison);
                    }
                });
                page.Footer().PaddingTop(12).AlignRight().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            }));
        }

        private static string FormatTime(double? value)
        {
            return value is double time && double.IsFinite(time) && time >= 0
                ? $"{time.ToString("N2", HungarianCulture)} ms"
                : "Nincs adat";
        }

        private static void SectionTitle(ColumnDescriptor column, string title)
        {
            column.Item().PaddingTop(8).EnsureSpace(140).Text(title).FontSize(15).SemiBold().FontColor(Colors.Blue.Darken3);
        }

        private static void Card(ColumnDescriptor column, string title, Action<ColumnDescriptor> content)
        {
            // A hosszú találat több oldalra törhet; nincs ShowEntire korlátozás.
            column.Item().EnsureSpace(100).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(body =>
            {
                body.Spacing(6);
                body.Item().Text(title).FontSize(12).SemiBold();
                content(body);
            });
        }

        private static void Field(ColumnDescriptor column, string label, string? value)
        {
            column.Item().Text(text =>
            {
                text.Span(label + ": ").SemiBold();
                text.Span(string.IsNullOrWhiteSpace(value) ? "Nincs adat" : value);
            });
        }
    }
}
