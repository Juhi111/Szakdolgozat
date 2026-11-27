using System;
using System.Linq;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.History;

namespace SqlPlanVisualizer.Core.Analysis
{
    /// <summary>A párosított elemzésből előzményként menthető bejegyzést készít.</summary>
    public class AnalysisHistoryMapper
    {
        public AnalysisHistoryEntry Map(
            CombinedAnalysisResult analysis,
            bool includeSourceFiles,
            string? sqlText,
            string? explainJson)
        {
            ArgumentNullException.ThrowIfNull(analysis);

            var paths = new Dictionary<PlanNode, string>();
            var pending = new Stack<(PlanNode Node, string Path)>();
            pending.Push((analysis.Pair.Plan.Root, "1"));
            while (pending.TryPop(out var item))
            {
                if (!paths.TryAdd(item.Node, item.Path)) continue;
                for (int i = item.Node.Children.Count - 1; i >= 0; i--)
                    pending.Push((item.Node.Children[i], $"{item.Path}.{i + 1}"));
            }
            string? PlanKey(PlanAnalysisFinding finding) => paths.TryGetValue(finding.Node, out var path) ? $"{finding.RuleId}:{path}" : null;

            return new AnalysisHistoryEntry
            {
                PlanOrigin = analysis.Pair.Plan.Origin is { } origin
                    ? origin with { SqlText = includeSourceFiles ? origin.SqlText : null }
                    : null,
                IndexAssessment = analysis.IndexAssessment,
                StatementIndex = analysis.Pair.Statement.StatementIndex,
                PlanIndex = analysis.Pair.Plan.PlanIndex,
                PlanningTime = analysis.PlanningTime,
                ExecutionTime = analysis.ExecutionTime,

                SqlText = includeSourceFiles ? sqlText : null,
                ExplainJson = includeSourceFiles ? explainJson : null,

                SqlFindings = analysis.SqlFindings.Select(finding => new HistoryFinding
                {
                    EvidenceKey = finding.SourceOffset is int offset ? $"{finding.RuleId}:{offset}" : null,
                    RuleId = finding.RuleId,
                    Title = finding.Title,
                    Description = finding.Description,
                    Recommendation = finding.Recommendation
                }).ToList(),

                PlanFindings = analysis.PlanFindings.Select(finding => new HistoryFinding
                {
                    EvidenceKey = PlanKey(finding),
                    NodePath = paths.GetValueOrDefault(finding.Node),
                    RuleId = finding.RuleId,
                    Title = finding.Title,
                    Description = finding.Description,
                    Recommendation = finding.Recommendation,
                    NodeType = finding.Node.NodeType,
                    RelationName = finding.Node.RelationName
                }).ToList(),

                CombinedFindings = analysis.CombinedFindings.Select(finding => new HistoryFinding
                {
                    RelatedSqlRuleId = finding.RelatedSqlRuleId,
                    RelatedPlanKey = PlanKey(finding.RelatedPlanFinding),
                    NodePath = paths.GetValueOrDefault(finding.RelatedPlanFinding.Node),
                    RuleId = finding.RuleId,
                    Title = finding.Title,
                    Description = finding.Description,
                    Recommendation = finding.Recommendation,
                    SqlObservation = finding.SqlObservation,
                    PlanObservation = finding.PlanObservation,
                    NodeType = finding.RelatedPlanFinding?.Node.NodeType,
                    RelationName = finding.RelatedPlanFinding?.Node.RelationName
                }).ToList()
            };
        }
    }
}