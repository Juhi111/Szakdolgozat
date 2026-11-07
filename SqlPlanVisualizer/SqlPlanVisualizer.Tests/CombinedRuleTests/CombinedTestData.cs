using System.Text.Json;
using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.CombinedRules;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql;
using Xunit;

namespace SqlPlanVisualizer.Tests.CombinedRuleTests;

internal static class CombinedTestData
{
    public static CombinedAnalysisResult Filter(string sql, string filter, string alias = "c", string table = "customers")
    {
        string json = JsonSerializer.Serialize(new[] { new Dictionary<string, object>
        {
            ["Plan"] = new Dictionary<string, object>
            {
                ["Node Type"] = "Seq Scan", ["Relation Name"] = table, ["Alias"] = alias,
                ["Plan Rows"] = 1, ["Actual Rows"] = 1, ["Actual Loops"] = 1,
                ["Rows Removed by Filter"] = 49999, ["Filter"] = filter
            }
        }});
        return Analyze(sql, json);
    }

    public static CombinedAnalysisResult Analyze(string sql, string json)
    {
        var parsed = new PostgreSqlQueryService().ParseSummary(sql);
        Assert.True(parsed.IsSuccess, parsed.ErrorMessage);
        var statement = Assert.Single(parsed.Statements);
        var plan = Assert.Single(new PostgreSqlPlanService().ParsePlans(json));
        var findings = new PlanAnalysisService(new IPlanAnalysisRule[]
        {
            new HighFilterRemovalRule(10000, 0.9), new SortSpillRule(), new HashAggregateSpillRule(),
            new LargeSortForLimitRule(100000, 100)
        }).Analyze(new[] { plan.Root });
        return new CombinedAnalysisService(CombinedRuleFactory.CreateDefault()).Analyze(
            new StatementPlanPair(statement, plan), new LargeOffsetRule().Analyze(statement), findings);
    }
}
