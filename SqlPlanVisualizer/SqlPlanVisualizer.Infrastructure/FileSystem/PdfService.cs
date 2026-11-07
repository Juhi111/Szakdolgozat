using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    /// <summary>A párosított elemzés teljes, többoldalas PDF-összegzését készíti el.</summary>
    public class PdfService
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
        public void SaveResultToPdf(CombinedAnalysisResult result, string path)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            string destination = Path.GetFullPath(path);
            string temporaryPath = Path.Combine(Path.GetDirectoryName(destination)!, $".sqlplan-{Guid.NewGuid():N}.tmp");

            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    CreateDocument(result).GeneratePdf(stream);
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

        private static IDocument CreateDocument(CombinedAnalysisResult result)
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
                        info.Item().Text($"{result.Pair.Statement.StatementIndex}. SQL-utasítás - {result.Pair.Plan.PlanIndex}. végrehajtási terv").SemiBold();
                        info.Item().Text($"Tervezési idő: {FormatTime(result.PlanningTime)}");
                        info.Item().Text($"Végrehajtási idő: {FormatTime(result.ExecutionTime)}");
                    });

                    SectionTitle(column, $"Összekapcsolt megállapítások ({result.CombinedFindings.Count})");
                    if (result.CombinedFindings.Count == 0)
                        column.Item().Text("Nem találtunk közvetlenül összekapcsolható megállapítást.");
                    foreach (var finding in result.CombinedFindings)
                    {
                        Card(column, finding.Title, body =>
                        {
                            Field(body, "SQL-megfigyelés", finding.SqlObservation);
                            Field(body, "Tervmegfigyelés", finding.PlanObservation);
                            Field(body, "Értékelés", finding.Description);
                            Field(body, "Javaslat", finding.Recommendation);
                        });
                    }

                    SectionTitle(column, $"SQL-találatok ({result.SqlFindings.Count})");
                    if (result.SqlFindings.Count == 0)
                        column.Item().Text("Ehhez az SQL-utasításhoz nem keletkezett találat.");
                    foreach (var finding in result.SqlFindings)
                    {
                        Card(column, finding.Title, body =>
                        {
                            Field(body, "Értékelés", finding.Description);
                            Field(body, "Javaslat", finding.Recommendation);
                        });
                    }

                    SectionTitle(column, $"Tervtalálatok ({result.PlanFindings.Count})");
                    if (result.PlanFindings.Count == 0)
                        column.Item().Text("Ehhez a tervhez nem keletkezett találat.");
                    foreach (var finding in result.PlanFindings)
                    {
                        Card(column, finding.Title, body =>
                        {
                            string relation = string.IsNullOrWhiteSpace(finding.Node.RelationName) ? string.Empty : $" ({finding.Node.RelationName})";
                            Field(body, "Művelet", finding.Node.NodeType + relation);
                            Field(body, "Értékelés", finding.Description);
                            Field(body, "Javaslat", finding.Recommendation);
                        });
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
