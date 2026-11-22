using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.FileSystem;

public partial class PdfService
{
    private static void ComposeComparison(ColumnDescriptor column, SqlRewriteComparison result)
    {
        column.Item().Text("SQL-javaslat és tervösszehasonlítás").FontSize(22).SemiBold().FontColor(Colors.Blue.Darken3);
        column.Item().Text(result.Suggestion.Title).FontSize(14).SemiBold();
        column.Item().Background(Colors.Amber.Lighten4).Padding(10).Text(result.EquivalenceStatus);
        Field(column, "Értékelés", result.TimingAssessment);
        Field(column, "Átírás magyarázata", result.Suggestion.Explanation);
        Field(column, "Alkalmazási feltételek", result.Suggestion.Preconditions);
        SectionTitle(column, "Eredeti SQL");
        column.Item().Background(Colors.Grey.Lighten4).Padding(10).Text(result.Suggestion.OriginalSql).FontSize(9);
        SectionTitle(column, "Javasolt SQL");
        column.Item().Background(Colors.Grey.Lighten4).Padding(10).Text(result.Suggestion.SuggestedSql).FontSize(9);
        Field(column, "Eredeti terv eredete", result.Original.Pair.Plan.Origin?.Description);
        Field(column, "Javasolt terv eredete", result.SuggestedPlan.Origin?.Description);
        SectionTitle(column, "Eredeti és javasolt terv adatai");
        var before = result.Original.Pair.Plan;
        var after = result.SuggestedPlan;
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(); columns.RelativeColumn(); });
            table.Header(header =>
            {
                foreach (string title in new[] { "Adat", "Eredeti", "Javasolt" })
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(6).Text(title).SemiBold();
            });
            void Row(string name, string original, string suggested)
            {
                foreach (string text in new[] { name, original, suggested })
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(text);
            }
            Row("Terv módja", CaptureMode(before.Origin?.Mode), CaptureMode(after.Origin?.Mode));
            Row("Tervezési idő", FormatTime(before.PlanningTime), FormatTime(after.PlanningTime));
            Row("Végrehajtási idő", FormatTime(before.ExecutionTime), FormatTime(after.ExecutionTime));
            Row("Gyökér becsült teljes költsége", Metric(before.Root.TotalCost), Metric(after.Root.TotalCost));
            Row("Gyökér tényleges sorai / végrehajtás", Metric(before.Root.ActualRows), Metric(after.Root.ActualRows));
            Row("SQL-találatok száma", result.Original.SqlFindings.Count.ToString(), result.SqlFindings.Count.ToString());
            Row("Tervtalálatok száma", result.Original.PlanFindings.Count.ToString(), result.PlanFindings.Count.ToString());
        });
        column.Item().Text("A költség nem időérték. A mérések eltérő időpontban készültek; az adatok, terhelés és gyorsítótár változhattak.").FontSize(9);
        PlanOutline(column, "Eredeti terv műveletei", before.Root);
        PlanOutline(column, "Javasolt terv műveletei", after.Root);
        if (result.SqlFindings.Count == 0)
        {
            SectionTitle(column, "Javasolt SQL találatai (0)");
            column.Item().Text("A javasolt SQL-re nem keletkezett találat.");
        }
        foreach (var finding in result.SqlFindings)
            Card(column, $"Javasolt SQL találata: {finding.Title}", body => { Field(body, "Értékelés", finding.Description); Field(body, "Javaslat", finding.Recommendation); });
        if (result.PlanFindings.Count == 0)
        {
            SectionTitle(column, "Javasolt terv találatai (0)");
            column.Item().Text("A javasolt tervre nem keletkezett találat.");
        }
        foreach (var finding in result.PlanFindings)
            Card(column, $"Javasolt terv találata: {finding.Title}", body =>
            {
                Field(body, "Művelet", finding.Node.NodeType + " " + finding.Node.RelationName);
                Field(body, "Értékelés", finding.Description);
                Field(body, "Javaslat", finding.Recommendation);
            });
    }

    private static void PlanOutline(ColumnDescriptor column, string title, PlanNode root)
    {
        SectionTitle(column, title);
        var stack = new Stack<(PlanNode Node, string Path)>();
        stack.Push((root, "1"));
        while (stack.Count > 0)
        {
            var (node, path) = stack.Pop();
            column.Item().Text($"{path}: {node.NodeType} {node.RelationName}");
            for (int i = node.Children.Count - 1; i >= 0; i--)
                stack.Push((node.Children[i], $"{path}.{i + 1}"));
        }
    }
    private static string Metric(double? value) => value is double number && double.IsFinite(number) ? number.ToString("N3", HungarianCulture) : "Nincs adat";
    private static string CaptureMode(PlanCaptureMode? mode) => mode switch { PlanCaptureMode.Actual => "Mért", PlanCaptureMode.Estimated => "Becsült", _ => "Ismeretlen" };
}
