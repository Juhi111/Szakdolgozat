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

                Assert.Equal(entry.Id, Assert.Single(result.Entries).Entry.Id);
                Assert.Contains("incomplete.json", Assert.Single(result.Errors));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public async Task DeleteAsync_UsesLoadedPathWithoutReadingOtherFiles()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                var service = new AnalysisHistoryService();
                var entry = new AnalysisHistoryEntry { StatementIndex = 1, PlanIndex = 1 };
                string original = await service.SaveAsync(entry, directory);
                string renamed = Path.Combine(directory, "renamed.json");
                File.Move(original, renamed);
                var loaded = Assert.Single((await service.LoadAllAsync(directory)).Entries);

                string unrelated = Path.Combine(directory, "unreadable.json");
                using (var locked = new FileStream(unrelated, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Assert.True(await service.DeleteAsync(loaded));
                    Assert.False(File.Exists(renamed));
                    Assert.True(File.Exists(unrelated));
                    Assert.False(await service.DeleteAsync(loaded));
                }
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public async Task DeleteAllAsync_PreservesInvalidFilesAndReturnsSameErrorAsReload()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                var service = new AnalysisHistoryService();
                await service.SaveAsync(new AnalysisHistoryEntry { StatementIndex = 1, PlanIndex = 1 }, directory);
                string invalid = Path.Combine(directory, "invalid.json");
                await File.WriteAllTextAsync(invalid, "{}");

                var deleted = await service.DeleteAllAsync(directory);
                var reloaded = await service.LoadAllAsync(directory);

                Assert.Equal(1, deleted.DeletedCount);
                Assert.Empty(reloaded.Entries);
                Assert.Equal(Assert.Single(deleted.Errors), Assert.Single(reloaded.Errors));
                Assert.Equal("{}", await File.ReadAllTextAsync(invalid));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }
    }
}
