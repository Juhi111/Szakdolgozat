using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Desktop.ViewModels;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using Xunit;

namespace SqlPlanVisualizer.Tests
{
    public sealed class DatabaseConnectionViewModelTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "SqlPlanVisualizerTests", Guid.NewGuid().ToString("N"));
        private string ProfilePath => Path.Combine(_directory, "database-connections.json");

        [Fact]
        public async Task SavedProfile_ReloadsWithoutSessionPasswordAndUsesFreshPassword()
        {
            var service = new DatabaseConnectionProfileService(ProfilePath);
            var connection = new FakeConnectionService();
            var session = new DatabaseSession();
            var vm = new DatabaseConnectionViewModel(connection, session, service);
            await vm.LoadProfilesAsync();
            vm.ProfileName = "Szakdolgozat";
            vm.Options.Database = "szakdoga_teszt";
            vm.Options.Username = "tester";
            Assert.True(await vm.UseConnectionAsync("old-session-secret"));
            await vm.SaveProfileAsync();
            Assert.DoesNotContain("old-session-secret", await File.ReadAllTextAsync(ProfilePath));

            var reopened = new DatabaseConnectionViewModel(connection, new DatabaseSession(), new DatabaseConnectionProfileService(ProfilePath));
            await reopened.LoadProfilesAsync();
            reopened.SelectedProfile = Assert.Single(reopened.Profiles);
            Assert.Equal("szakdoga_teszt", reopened.Options.Database);
            reopened.Options.Database = "edited";
            Assert.Equal("szakdoga_teszt", reopened.SelectedProfile.Options.Database);
            Assert.Equal("szakdoga_teszt", session.GetConnectionDetails().Options.Database);
            Assert.True(await reopened.UseConnectionAsync("new-password"));
            Assert.Equal("new-password", connection.LastPassword);
        }

        [Fact]
        public async Task FailedLoad_DisablesSavingAndPreservesFile()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(ProfilePath, "broken");
            var vm = new DatabaseConnectionViewModel(new FakeConnectionService(), new DatabaseSession(), new DatabaseConnectionProfileService(ProfilePath));
            await vm.LoadProfilesAsync();
            Assert.False(vm.CanManageProfiles);
            await vm.SaveProfileAsync();
            Assert.Equal("broken", await File.ReadAllTextAsync(ProfilePath));
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task DeleteProfile_DoesNotForgetActiveSession()
        {
            var session = new DatabaseSession();
            var vm = new DatabaseConnectionViewModel(new FakeConnectionService(), session, new DatabaseConnectionProfileService(ProfilePath));
            await vm.LoadProfilesAsync();
            vm.ProfileName = "Helyi";
            vm.Options.Database = "test";
            vm.Options.Username = "tester";
            await vm.UseConnectionAsync("secret");
            await vm.SaveProfileAsync();
            await vm.DeleteProfileAsync();
            Assert.Empty(vm.Profiles);
            Assert.Null(vm.SelectedProfile);
            Assert.True(session.HasConfiguration);
            Assert.Equal("test", session.GetConnectionDetails().Options.Database);
        }

        private sealed class FakeConnectionService : IDatabaseConnectionService
        {
            public string? LastPassword { get; private set; }
            public Task<DatabaseConnectionInfo> TestConnectionAsync(DatabaseConnectionOptions options, string password, CancellationToken cancellationToken = default)
            {
                LastPassword = password;
                return Task.FromResult(new DatabaseConnectionInfo { DatabaseName = options.Database, Username = options.Username, ServerVersion = "test" });
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
