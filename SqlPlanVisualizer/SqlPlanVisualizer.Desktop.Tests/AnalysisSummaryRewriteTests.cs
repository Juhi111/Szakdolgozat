using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Desktop.ViewModels;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;
using SqlPlanVisualizer.Desktop.Configuration;
using SqlPlanVisualizer.Desktop.Services;

namespace SqlPlanVisualizer.Desktop.Tests
{
    public class AnalysisSummaryRewriteTests
    {
        [Fact]
        public void Summary_UsesSelectedStatementSnapshotWithoutChangingOriginalResult()
        {
            const string document = "SELECT 1; SELECT order_id FROM orders WHERE customer_id + 1 = 101;";
            var statement = new PostgreSqlQueryService().ParseSummary(document).Statements[1];
            var result = CreateResult(statement);
            var viewModel = new AnalysisSummaryViewModel(result, document);

            var suggestion = Assert.Single(viewModel.RewriteSuggestions);
            Assert.Contains("customer_id = 100", suggestion.SuggestedSql);
            Assert.DoesNotContain("SELECT 1", suggestion.OriginalSql);
            Assert.Same(result, viewModel.Result);
            Assert.Contains("(1)", viewModel.RewriteHeader);
        }

        [Fact]
        public void Summary_WithoutSourceOrWithInvalidSource_RemainsAvailable()
        {
            var result = CreateResult(new SqlStatementSummary { StatementIndex = 1, TextStart = 0, TextLength = 3 });
            Assert.Empty(new AnalysisSummaryViewModel(result).RewriteSuggestions);
            var invalid = new AnalysisSummaryViewModel(result, "???");
            Assert.Empty(invalid.RewriteSuggestions);
            Assert.Contains("nem dolgozható fel", invalid.RewriteStatus);
            Assert.Same(result, invalid.Result);
        }

        [Fact]
        public void Summary_RegistersNewRewritesAndSqlAnalysisRules()
        {
            const string sql = "SELECT x FROM t WHERE x IN (1)";
            var statement = new PostgreSqlQueryService().ParseSummary(sql).Statements[0];
            var viewModel = new AnalysisSummaryViewModel(CreateResult(statement), sql);
            Assert.Equal("SINGLETON_IN_REWRITE", Assert.Single(viewModel.RewriteSuggestions).RuleId);
            var settings = new AppSettings
            {
                SqlAnalysis = new SqlAnalysisSettings { LargeOffsetThreshold = 10000 },
                PlanAnalysis = new PlanAnalysisSettings
                {
                    RowEstimateMismatch = new() { RatioThreshold = 10, DifferenceThreshold = 1000 },
                    HighFilterRemoval = new() { MinimumRemovedRows = 1000, RemovalRatioThreshold = 0.9 },
                    HighHeapFetches = new() { MinimumHeapFetches = 1000 },
                    RepeatedSequentialScan = new() { MinimumLoops = 10, MinimumExaminedRows = 1000 },
                    LargeSortForLimit = new() { MinimumInputRows = 1000, InputOutputRatioThreshold = 10 }
                }
            };
            var service = AnalysisServiceFactory.CreateSqlAnalysisService(settings);
            Assert.Contains(service.Analyze(new[] { statement }), f => f.RuleId == "SINGLETON_IN");
        }

        [Fact]
        public void Summary_GeneratesRelationalRewriteFromAutomaticMetadata()
        {
            const string sql = "SELECT c.id FROM lab.customers c WHERE c.id NOT IN (SELECT o.id FROM lab.orders o)";
            var statement = new PostgreSqlQueryService().ParseSummary(sql).Statements[0];
            var result = CreateResult(statement);
            Assert.Empty(new AnalysisSummaryViewModel(result, sql).RewriteSuggestions);
            var tables = new[] { "customers", "orders" }.Select(name => new SqlPlanVisualizer.Core.Models.Database.TableIndexReport(
                new SqlPlanVisualizer.Core.Models.Database.DatabaseTableMetadata
                {
                    Schema = "lab", Table = name, RelationKind = "r", HasDescendants = false, RowSecurity = false,
                    Columns = [new() { Name = "id", TypeName = "int8", TypeSchema = "pg_catalog", IsNullable = false }]
                }, Array.Empty<SqlPlanVisualizer.Core.Models.Database.IndexObservation>())).ToArray();
            result.SetDatabaseMetadata(new(DateTimeOffset.UtcNow, "test", tables, []));
            var viewModel = new AnalysisSummaryViewModel(result, sql);
            var suggestion = Assert.Single(viewModel.RewriteSuggestions);
            Assert.Equal("NOT_IN_EXISTS_REWRITE", suggestion.RuleId);
            Assert.True(suggestion.RequiresRelationalMetadata);
        }

        private static CombinedAnalysisResult CreateResult(SqlStatementSummary statement) =>
            new(new StatementPlanPair(statement, new ExecutionPlan(1, new PlanNode { NodeType = "Seq Scan" })),
                Array.Empty<SqlAnalysisFinding>(), Array.Empty<PlanAnalysisFinding>());
    }
}
