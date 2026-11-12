using System.Text.Json;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using Xunit;

namespace SqlPlanVisualizer.Tests.FileSystemTests;

public class UserSettingsServiceTests
{
    [Fact]
    public async Task ResetAsync_BacksUpInvalidSettingsAndRestoresUsableDefaults()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "user-settings.json");
            const string invalid = "{invalid settings";
            await File.WriteAllTextAsync(path, invalid);
            var service = new UserSettingsService(path);
            await Assert.ThrowsAsync<JsonException>(() => service.LoadAsync());

            var reset = await service.ResetAsync();
            var settings = await service.LoadAsync();

            Assert.NotNull(reset.BackupPath);
            Assert.Equal(invalid, await File.ReadAllTextAsync(reset.BackupPath!));
            Assert.False(settings.SaveAnalysisHistory);
            Assert.False(settings.IncludeSourceFiles);
            Assert.True(Path.IsPathFullyQualified(settings.HistoryDirectory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
