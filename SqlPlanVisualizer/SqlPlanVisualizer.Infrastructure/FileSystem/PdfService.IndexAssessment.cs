using QuestPDF.Fluent;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.FileSystem;

public partial class PdfService
{
    private static void ComposeIndexAssessment(ColumnDescriptor column, IndexAssessmentSnapshot? snapshot)
    {
        SectionTitle(column, "Adatbázis alapján pontosított indexjavaslatok – eredeti terv");
        if (snapshot is null) { column.Item().Text(IndexAssessmentSnapshot.NotChecked); return; }
        column.Item().Text(snapshot.SourceText);
        column.Item().Text(snapshot.Guidance);
        foreach (var notice in snapshot.Notices) column.Item().Text(notice);
        if (snapshot.Items.Count == 0) column.Item().Text("Nem keletkezett indexillesztési megállapítás. Ez nem jelenti, hogy nincs megfelelő index.");
        foreach (var item in snapshot.Items)
            Card(column, item.Table + " – " + item.Title, body => Field(body, "Értékelés és javaslat", item.Description));
        if (snapshot.Evidence.Count > 0)
        {
            column.Item().PaddingTop(8).Text("A lekért index- és kulcsdefiníciók").SemiBold();
            foreach (var evidence in snapshot.Evidence) column.Item().Text(evidence).FontSize(8);
        }
    }
}
