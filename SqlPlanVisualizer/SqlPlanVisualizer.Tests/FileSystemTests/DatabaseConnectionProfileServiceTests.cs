using System.Text.Json;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using Xunit;

namespace SqlPlanVisualizer.Tests.FileSystemTests
{
    public sealed class DatabaseConnectionProfileServiceTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "SqlPlanVisualizerTests", Guid.NewGuid().ToString("N"));
        private string ProfilePath => Path.Combine(_directory, "database-connections.json");

        private static DatabaseConnectionProfile Profile(string name = "Helyi teszt") => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Options = new DatabaseConnectionOptions
            {
                Host = "localhost", Port = 5433, Database = "szakdoga_teszt", Username = "tester",
                Schema = "szakdoga_lab", TlsMode = DatabaseTlsMode.Disable
            }
        };

        [Fact]
        public async Task SaveReloadRenameAndDelete_PreservesOtherProfilesAndAllSettings()
        {
            var service = new DatabaseConnectionProfileService(ProfilePath);
            Assert.Empty(await service.LoadAsync());
            var first = Profile();
            var second = Profile("Másik szerver");
            await service.SaveAsync(first);
            await service.SaveAsync(second);

            var reloaded = new DatabaseConnectionProfileService(ProfilePath);
            var result = (await reloaded.LoadAsync()).Single(item => item.Id == first.Id);
            Assert.Equal(first.Name, result.Name);
            Assert.Equal(JsonSerializer.Serialize(first.Options), JsonSerializer.Serialize(result.Options));
            first.Name = "Átnevezett kapcsolat";
            first.Options.Port = 5434;
            await reloaded.SaveAsync(first);
            Assert.Equal(2, (await reloaded.LoadAsync()).Count);
            Assert.Equal(5434, (await reloaded.LoadAsync()).Single(item => item.Id == first.Id).Options.Port);
            await reloaded.DeleteAsync(first.Id);
            Assert.Equal(second.Id, Assert.Single(await reloaded.LoadAsync()).Id);
            Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        }

        [Fact]
        public async Task DuplicateNameOrInvalidPort_DoesNotChangeExistingFile()
        {
            var service = new DatabaseConnectionProfileService(ProfilePath);
            await service.SaveAsync(Profile("Teszt"));
            string before = await File.ReadAllTextAsync(ProfilePath);
            await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(Profile(" TESZT ")));
            var invalid = Profile("Új");
            invalid.Options.Port = 70000;
            await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(invalid));
            Assert.Equal(before, await File.ReadAllTextAsync(ProfilePath));
        }

        [Theory]
        [InlineData("broken json")]
        [InlineData("{\"FormatVersion\":2,\"Profiles\":[]}")]
        [InlineData("{\"FormatVersion\":1,\"Profiles\":[null]}")]
        public async Task InvalidFile_IsNotOverwrittenBySaveOrDelete(string json)
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(ProfilePath, json);
            var service = new DatabaseConnectionProfileService(ProfilePath);
            await Assert.ThrowsAsync<JsonException>(() => service.LoadAsync());
            await Assert.ThrowsAsync<JsonException>(() => service.SaveAsync(Profile()));
            await Assert.ThrowsAsync<JsonException>(() => service.DeleteAsync(Guid.NewGuid()));
            Assert.Equal(json, await File.ReadAllTextAsync(ProfilePath));
        }

        [Fact]
        public async Task SerializedProfile_ContainsOnlyExpectedNonSecretProperties()
        {
            var service = new DatabaseConnectionProfileService(ProfilePath);
            await service.SaveAsync(Profile());
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(ProfilePath));
            var profile = json.RootElement.GetProperty("Profiles")[0];
            Assert.Equal(new[] { "Id", "Name", "Options" }, profile.EnumerateObject().Select(item => item.Name));
            Assert.Equal(new[] { "Host", "Port", "Database", "Username", "Schema", "TlsMode" },
                profile.GetProperty("Options").EnumerateObject().Select(item => item.Name));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
