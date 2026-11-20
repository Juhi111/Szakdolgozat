using System.Windows.Input;
using Npgsql;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Desktop.Configuration;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Desktop.ViewModels;
using Xunit;

namespace SqlPlanVisualizer.Desktop.Tests;

public sealed class ExplainCommandTests
{
    private const string PlanJson = """
        [{"Plan":{"Node Type":"Result","Plan Rows":1,"Actual Rows":1,"Actual Loops":1,"Actual Total Time":0.1}}]
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FetchCommand_RoutesToRequestedModeAndLoadsWithoutAnalysis(bool actual)
    {
        var service = new FakeExplainService();
        var vm = Create(service);
        var command = actual ? vm.FetchActualPlanCommand : vm.FetchEstimatedPlanCommand;
        Task completion = ExecuteAsync(command);

        Assert.True(vm.IsFetchingExplain);
        Assert.False(vm.CanFetchExplain);
        Assert.Equal(actual, service.Actual);
        Assert.Equal("SELECT 1;", service.Sql);
        // The other command must not start a second request.
        (actual ? vm.FetchEstimatedPlanCommand : vm.FetchActualPlanCommand).Execute(null);
        Assert.Equal(1, service.Calls);

        service.Response.SetResult(PlanJson);
        await completion;

        Assert.Equal("Result", Assert.Single(vm.PlanRoots).NodeType);
        Assert.Empty(vm.PlanSuggestions);
        Assert.Empty(vm.PlansForSummary);
        Assert.Empty(vm.Suggestions);
        Assert.False(vm.IsFetchingExplain);
        Assert.True(vm.CanFetchExplain);
    }

    [Fact]
    public async Task FailedRequest_ShowsSqlStateAndPreservesPreviousPlan()
    {
        var service = new FakeExplainService();
        var vm = Create(service);
        var original = new SqlPlanVisualizer.Core.Models.PlanNode { NodeType = "Result" };
        vm.PlanRoots.Add(original);
        Task completion = ExecuteAsync(vm.FetchActualPlanCommand);
        service.Response.SetException(new PostgresException("relation does not exist", "ERROR", "ERROR", "42P01"));
        await completion;
        Assert.Same(original, Assert.Single(vm.PlanRoots));
        Assert.Contains("42P01", vm.StatusMessage);
        Assert.Contains("relation does not exist", vm.StatusMessage);
        Assert.True(vm.CanFetchExplain);
    }

    [Fact]
    public async Task ChangedSql_DiscardsPendingPlan()
    {
        var service = new FakeExplainService();
        var vm = Create(service);
        Task completion = ExecuteAsync(vm.FetchActualPlanCommand);
        vm.SqlText = "SELECT 2;";
        service.Response.SetResult(PlanJson);
        await completion;
        Assert.Empty(vm.PlanRoots);
        Assert.Contains("nem töltöttük be", vm.StatusMessage);
        Assert.True(vm.CanFetchExplain);
    }

    [Fact]
    public async Task MissingConnection_ReportsReasonWithoutCallingService()
    {
        var service = new FakeExplainService();
        var vm = new MainViewModel(AppSettingsLoader.Load(), new DatabaseSession(), service) { SqlText = "SELECT 1;" };
        await ExecuteAsync(vm.FetchActualPlanCommand);
        Assert.Equal(0, service.Calls);
        Assert.Contains("válassz adatbázis-kapcsolatot", vm.StatusMessage);
    }

    private static MainViewModel Create(FakeExplainService service)
    {
        var session = new DatabaseSession();
        session.Set(new DatabaseConnectionOptions { Database = "test", Username = "tester" }, "test-only-password");
        return new MainViewModel(AppSettingsLoader.Load(), session, service) { SqlText = "SELECT 1;" };
    }

    private static Task ExecuteAsync(ICommand command)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            if (!command.CanExecute(null))
                return;
            command.CanExecuteChanged -= handler;
            completion.TrySetResult();
        };
        command.CanExecuteChanged += handler;
        command.Execute(null);
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class FakeExplainService : IDatabaseExplainService
    {
        public TaskCompletionSource<string> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Actual { get; private set; }
        public string? Sql { get; private set; }
        public int Calls { get; private set; }
        public Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default) => Fetch(false, sql);
        public Task<string> GetActualPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default) => Fetch(true, sql);
        private Task<string> Fetch(bool actual, string sql)
        {
            Calls++;
            Actual = actual;
            Sql = sql;
            return Response.Task;
        }
    }
}
