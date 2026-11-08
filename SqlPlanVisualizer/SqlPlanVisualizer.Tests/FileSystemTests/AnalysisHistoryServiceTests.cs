using System.Text.Json.Nodes;
using SqlPlanVisualizer.Core.Models.History;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using Xunit;

namespace SqlPlanVisualizer.Tests.FileSystemTests
{
    public class AnalysisHistoryServiceTests
    {
        [Theory]
        [InlineData("SqlFindings")]
        [InlineData("PlanFindings")]
        [InlineData("CombinedFindings")]
        public async Task LoadAllAsync_MissingFindingArray_ReportsErrorAndKeepsValidEntry(string field)
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                var service = new AnalysisHistoryService();
                var entry = new AnalysisHistoryEntry { StatementIndex = 1, PlanIndex = 1 };
                string path = await service.SaveAsync(entry, directory);
                var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
                json.Remove(field);
                await File.WriteAllTextAsync(Path.Combine(directory, "incomplete.json"), json.ToJsonString());

                var result = await service.LoadAllAsync(directory);

                Assert.Equal(entry.Id, Assert.Single(result.Entries).Id);
                Assert.Contains("incomplete.json", Assert.Single(result.Errors));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }
    }
}
