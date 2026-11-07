using System.Text;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using Xunit;

namespace SqlPlanVisualizer.Tests.FileSystemTests
{
    public class PdfServiceTests
    {
        [Fact]
        public void SaveResultToPdf_ReplacesFileAndCleansUpAfterWriteFailure()
        {
            string directory = Path.Combine(Path.GetTempPath(), "sqlplan-pdf-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var service = new PdfService();
                string path = Path.Combine(directory, "elemzés.pdf");
                File.WriteAllText(path, "Korábbi dokumentum");
                service.SaveResultToPdf(CreateResult(), path);

                byte[] bytes = File.ReadAllBytes(path);
                Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
                Assert.Contains("%%EOF", Encoding.ASCII.GetString(bytes[^32..]));
                Assert.Empty(Directory.GetFiles(directory, ".sqlplan-*.tmp"));

                // A cél helyén könyvtár van: a csere hibázik, de a tartalma megmarad.
                string blockedPath = Path.Combine(directory, "foglalt.pdf");
                Directory.CreateDirectory(blockedPath);
                File.WriteAllText(Path.Combine(blockedPath, "megőrzendő.txt"), "Eredeti tartalom");
                var error = Record.Exception(() => service.SaveResultToPdf(CreateResult(), blockedPath));
                Assert.True(error is IOException or UnauthorizedAccessException);
                Assert.Equal("Eredeti tartalom", File.ReadAllText(Path.Combine(blockedPath, "megőrzendő.txt")));
                Assert.Empty(Directory.GetFiles(directory, ".sqlplan-*.tmp"));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static CombinedAnalysisResult CreateResult(int repetitions = 1, bool empty = false)
        {
            var node = new PlanNode { NodeType = "Seq Scan", RelationName = "customers" };
            var plan = new ExecutionPlan(2, node) { PlanningTime = empty ? null : 0.35, ExecutionTime = empty ? null : 24.2 };
            var pair = new StatementPlanPair(new SqlStatementSummary { StatementIndex = 3, StatementType = "SELECT" }, plan);
            var sqlFinding = new SqlAnalysisFinding
            {
                StatementIndex = 3, RuleId = "FUNCTION_ON_COLUMN", Title = "Függvény az oszlopon",
                Description = "A WHERE feltételben a lower(email) kifejezés szerepel.",
                Recommendation = "Ellenőrizd, szükséges-e a kisbetűsítés, és mérd meg a megfelelő kifejezésindex hatását."
            };
            var planFinding = new PlanAnalysisFinding(node)
            {
                PlanIndex = 2, RuleId = "HIGH_FILTER_REMOVAL", Title = "Sok kiszűrt sor",
                Description = "A customers tábla olvasásakor 49 999 sor kiesett, és egy sor maradt meg.",
                Recommendation = "Vizsgáld meg a szűrés szelektivitását és a művelet futási idejét."
            };
            var result = new CombinedAnalysisResult(pair,
                empty ? Array.Empty<SqlAnalysisFinding>() : new[] { sqlFinding },
                empty ? Array.Empty<PlanAnalysisFinding>() : new[] { planFinding });
            if (!empty)
            {
                result.CombinedFindings.Add(new CombinedAnalysisFinding
                {
                    RuleId = "FUNCTION_FILTER_WITH_HIGH_REMOVAL", Title = "Függvényes szűrés sok eldobott sorral",
                    SqlObservation = "A felső szintű WHERE a customers tábla email oszlopát kisbetűsítve vizsgálja.",
                    PlanObservation = "A terv Filter mezőjében ugyanaz a kifejezés található, és sok sor kiesett.",
                    Description = string.Join(" ", Enumerable.Repeat(
                        "Az SQL és a végrehajtási terv ugyanazt a szűrést jelzi. Ez önmagában nem bizonyítja, hogy a függvény okozta a lassulást. Árvíztűrő tükörfúrógép.", repetitions)),
                    Recommendation = "Gyakran futó lekérdezésnél hasonlítsd össze a megfelelő kifejezésindexszel mért futási időt.",
                    RelatedPlanFinding = planFinding
                });
            }
            return result;
        }
    }
}
