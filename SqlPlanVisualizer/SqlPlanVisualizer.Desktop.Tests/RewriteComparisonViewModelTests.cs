using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Desktop.Services;
using SqlPlanVisualizer.Desktop.ViewModels;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Desktop.Tests;

public class RewriteComparisonViewModelTests
{
    private static RewriteComparisonViewModel Create(FakeExplain fake, Func<bool> confirm)
    {
        var session = new DatabaseSession();
        session.Set(new DatabaseConnectionOptions { Host = "localhost", Database = "test", Username = "reader", Schema = "lab" }, "secret");
        var suggestion = new SqlRewriteSuggestion { RuleId = "test", Title = "test", OriginalSql = "SELECT 1", SuggestedSql = "SELECT 2", Explanation = "test", Preconditions = "test" };
        var original = new CombinedAnalysisResult(new StatementPlanPair(new SqlStatementSummary { StatementIndex = 1 },
            new ExecutionPlan(1, new PlanNode()) { Origin = new PlanOrigin { Source = PlanSourceKind.Database, SqlText = "SELECT 1",
                StatementIndex = 1, Host = "localhost", Port = 5432, Database = "test", Username = "reader", Schema = "lab" } }),
            Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>());
        return new RewriteComparisonViewModel(original, new[] { suggestion }, session,
            new SqlRewriteComparisonService(fake, new SqlAnalysisService(Array.Empty<ISqlAnalysisRule>()), new PlanAnalysisService(Array.Empty<IPlanAnalysisRule>())), confirm);
    }

    [Fact]
    public async Task DecliningActualRun_DoesNotContactDatabase()
    {
        var fake = new FakeExplain();
        var vm = Create(fake, () => false);
        await vm.FetchAsync(true);
        Assert.Equal(0, fake.Calls);
        Assert.False(vm.IsBusy);
        Assert.Null(vm.Comparison);
    }

    [Fact]
    public async Task PendingRun_PreventsSecondRunAndSelectionChange_AndCanBeCancelled()
    {
        var fake = new FakeExplain();
        var vm = Create(fake, () => true);
        var selected = vm.SelectedSuggestion;
        var running = vm.FetchAsync(false);
        Assert.True(vm.IsBusy);
        vm.SelectedSuggestion = null;
        Assert.Same(selected, vm.SelectedSuggestion);
        await vm.FetchAsync(true);
        Assert.Equal(1, fake.Calls);
        vm.Cancel();
        await running;
        Assert.False(vm.IsBusy);
        Assert.True(vm.CanStart);
        Assert.Null(vm.Comparison);
        Assert.Contains("megszakítottuk", vm.Status);
    }

    [Fact]
    public async Task SuccessfulEstimatedRun_PublishesComparisonWithoutConfirmation()
    {
        var fake = new FakeExplain();
        var vm = Create(fake, () => throw new Exception("Estimated plan must not prompt for actual execution"));
        var running = vm.FetchAsync(false);
        fake.Response.SetResult("[{\"Plan\":{\"Node Type\":\"Result\"}}]");
        await running;
        Assert.NotNull(vm.Comparison);
        Assert.NotEmpty(vm.Metrics);
        Assert.Single(vm.SuggestedRoots);
        Assert.False(vm.IsBusy);
    }

    private sealed class FakeExplain : IDatabaseExplainService
    {
        public int Calls { get; private set; }
        public TaskCompletionSource<string> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<string> GetEstimatedPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default)
        { Calls++; return Response.Task.WaitAsync(cancellationToken); }
        public Task<string> GetActualPlanAsync(DatabaseConnectionOptions options, string password, string sql, CancellationToken cancellationToken = default) =>
            GetEstimatedPlanAsync(options, password, sql, cancellationToken);
    }
}
