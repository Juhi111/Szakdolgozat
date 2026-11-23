using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Core.Models.History;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests;

public class IndexAssessmentSnapshotTests
{
    private static CombinedAnalysisResult Result() => new(new(new() { StatementIndex = 1 }, new(1, new() { NodeType = "Index Scan" })), [], []);
    private static IndexMetadataReport Report()
    {
        var index = new DatabaseIndexMetadata { Name = "idx_email", Definition = "CREATE INDEX idx_email ON lab.customers (lower(email))", Method = "btree", IsLive = true, IsReady = true, IsValid = true, Elements = [new() { Definition = "lower(email)" }] };
        var table = new DatabaseTableMetadata { Schema = "lab", Table = "customers", Indexes = [index] };
        var node = new PlanNode { NodeType = "Index Scan", RelationName = "customers" };
        node.AdditionalProperties["Schema"] = JsonSerializer.SerializeToElement("lab");
        node.AdditionalProperties["Index Name"] = JsonSerializer.SerializeToElement("idx_email");
        return new(DateTimeOffset.Parse("2026-11-23T14:00:00+01:00"), "localhost:5432/db; felhasználó: test",
            [new(table, IndexMetadataAnalysisService.Evaluate(table, "SELECT * FROM lab.customers WHERE lower(email)='a'", node))], ["Másik tábla metaadata nem elérhető."]);
    }

    [Fact]
    public void Snapshot_DoesNotChangeWhenReportOrResultIsRefreshed()
    {
        var report = Report(); var snapshot = IndexAssessmentSnapshot.Capture(report);
        var result = Result(); result.SetIndexAssessment(snapshot);
        var entry = new AnalysisHistoryMapper().Map(result, false, null, null);
        report.Tables[0].Metadata.Indexes.Clear();
        result.SetIndexAssessment(new(DateTimeOffset.Now, "new", [], []));
        Assert.Contains("CREATE INDEX", Assert.Single(snapshot.Evidence));
        Assert.Same(snapshot, entry.IndexAssessment);
        Assert.NotSame(entry.IndexAssessment, result.IndexAssessment);
    }

    [Fact]
    public void PlanNamedCandidate_ProducesSpecificGuidance()
    {
        var observation = Report().Tables[0].Observations.Single(o => o.Title.StartsWith("Függvényes"));
        Assert.Contains("A terv már megnevez", observation.Description);
        Assert.Contains("nem bizonyítja", observation.Description);
    }

    [Fact]
    public async Task History_RoundTripsSnapshotAndReadsLegacyWithoutMetadata()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var service = new AnalysisHistoryService(); var result = Result();
            var legacy = new AnalysisHistoryMapper().Map(result, false, null, null);
            Assert.Null((await service.LoadAsync(await service.SaveAsync(legacy, directory))).IndexAssessment);
            result.SetIndexAssessment(IndexAssessmentSnapshot.Capture(Report()));
            var entry = new AnalysisHistoryMapper().Map(result, false, null, null);
            var restored = await service.LoadAsync(await service.SaveAsync(entry, directory));
            Assert.Equal(entry.IndexAssessment!.Source, restored.IndexAssessment!.Source);
            Assert.Equal(entry.IndexAssessment.CapturedAt, restored.IndexAssessment.CapturedAt);
            Assert.Equal(entry.IndexAssessment.Items, restored.IndexAssessment.Items);
            Assert.Equal(entry.IndexAssessment.Notices, restored.IndexAssessment.Notices);
            Assert.Equal(entry.IndexAssessment.Evidence, restored.IndexAssessment.Evidence);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void Snapshot_RejectsNullCollections()
    {
        Assert.Throws<JsonException>(() => new IndexAssessmentSnapshot(default, "x", null!, []));
        Assert.Throws<JsonException>(() => new IndexAssessmentSnapshot(default, "x", [], [null!]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Pdf_ExportsWithAndWithoutSnapshot(bool withMetadata)
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var result = Result();
            if (withMetadata) result.SetIndexAssessment(IndexAssessmentSnapshot.Capture(Report()));
            string path = Path.Combine(directory, "index-summary.pdf");
            new PdfService().SaveResultToPdf(result, path);
            Assert.True(new FileInfo(path).Length > 1000);
            if (Environment.GetEnvironmentVariable("SQLPLAN_PDF_REVIEW_DIR") is string output)
            { Directory.CreateDirectory(output); File.Copy(path, Path.Combine(output, withMetadata ? "index-aware.pdf" : "index-unchecked.pdf"), true); }
        }
        finally { Directory.Delete(directory, true); }
    }
}
