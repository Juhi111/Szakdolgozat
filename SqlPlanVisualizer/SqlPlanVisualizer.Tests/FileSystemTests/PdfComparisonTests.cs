using System.Text;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using Xunit;

namespace SqlPlanVisualizer.Tests.FileSystemTests;

public class PdfComparisonTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExportComparison_SupportsMissingTimesAndLongMultiPageContent(bool measured)
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var comparison = CreateComparison(measured);
            string path = Path.Combine(directory, "comparison.pdf");
            new PdfService().SaveResultToPdf(comparison.Original, path, comparison);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            Assert.Contains("%%EOF", Encoding.ASCII.GetString(bytes[^32..]));
            Assert.Empty(Directory.GetFiles(directory, ".sqlplan-*.tmp"));
            // Opcionális kimenet kézi, renderelt PDF-ellenőrzéshez; a normál teszt nem hagy fájlt.
            if (Environment.GetEnvironmentVariable("SQLPLAN_PDF_REVIEW_DIR") is string reviewDirectory)
            {
                Directory.CreateDirectory(reviewDirectory);
                File.Copy(path, Path.Combine(reviewDirectory, measured ? "measured.pdf" : "estimated.pdf"), true);
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void ExportComparison_RejectsUnrelatedOriginalWithoutReplacingExistingFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "existing");
            var comparison = CreateComparison(true);
            Assert.Throws<ArgumentException>(() => new PdfService().SaveResultToPdf(CreateComparison(false).Original, path, comparison));
            Assert.Equal("existing", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    private static SqlRewriteComparison CreateComparison(bool measured)
    {
        var origin = new PlanOrigin { Source = PlanSourceKind.Database, Mode = PlanCaptureMode.Actual,
            Host = "localhost", Port = 5432, Database = "szakdoga_teszt", Schema = "szakdoga_lab", Username = "reader",
            CapturedAt = new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero), StatementIndex = 1 };
        var original = new CombinedAnalysisResult(new StatementPlanPair(new SqlStatementSummary { StatementIndex = 1 },
            new ExecutionPlan(1, new PlanNode { NodeType = "Seq Scan", RelationName = "orders", TotalCost = 12000, ActualRows = 5 })
            { Origin = origin, ExecutionTime = 29.96, PlanningTime = 0.34 }), Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>());
        var root = new PlanNode { NodeType = "Index Scan", RelationName = "orders", TotalCost = 8.5, ActualRows = measured ? 5 : null };
        var plan = new ExecutionPlan(1, root) { Origin = origin with { Mode = measured ? PlanCaptureMode.Actual : PlanCaptureMode.Estimated,
            CapturedAt = origin.CapturedAt.AddMinutes(1) }, ExecutionTime = measured ? 0.06 : null, PlanningTime = measured ? 0.2 : null };
        string sql = "SELECT order_id, customer_id FROM szakdoga_lab.orders WHERE customer_id + 1 = 101;";
        var suggestion = new SqlRewriteSuggestion { RuleId = "ARITHMETIC_EQUALITY_REWRITE", Title = "Összeadás áthelyezése",
            OriginalSql = sql + "\n" + string.Join("\n", Enumerable.Repeat("-- Hosszú SQL: árvíztűrő tükörfúrógép, megőrzött megjegyzés.", 40)),
            SuggestedSql = "SELECT order_id, customer_id FROM szakdoga_lab.orders WHERE customer_id = 100;",
            Explanation = "Az oszlopon végzett összeadást a konstans oldalra helyeztük.", Preconditions = "Ellenőrizd az adattípust és a túlcsordulás lehetőségét." };
        var finding = new PlanAnalysisFinding(root) { Title = "Teszt megállapítás", Description = string.Join(" ", Enumerable.Repeat("Hosszú magyar leírás több oldalon. Árvíztűrő tükörfúrógép.", 80)), Recommendation = "Ellenőrizd az eredményeket." };
        return new(original, suggestion, plan, Array.Empty<SqlAnalysisFinding>(), new[] { finding })
        {
            MetadataCheck = new(MetadataCheckStatus.Passed, "A típusfeltétel teljesül. A túlcsordulás nincs ellenőrizve.",
                new("szakdoga_lab", "orders", "customer_id", "pg_catalog", "int8", "bigint", false, false),
                origin.CapturedAt, "localhost:5432/szakdoga_teszt; felhasználó: reader")
        };
    }
}
