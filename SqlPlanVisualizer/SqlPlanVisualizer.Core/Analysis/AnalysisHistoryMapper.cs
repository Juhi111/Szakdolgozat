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

            return new AnalysisHistoryEntry
            {
                StatementIndex = analysis.Pair.Statement.StatementIndex,
                PlanIndex = analysis.Pair.Plan.PlanIndex,
                PlanningTime = analysis.PlanningTime,
                ExecutionTime = analysis.ExecutionTime,

                SqlText = includeSourceFiles ? sqlText : null,
                ExplainJson = includeSourceFiles ? explainJson : null,

                SqlFindings = analysis.SqlFindings.Select(finding => new HistoryFinding
                {
                    RuleId = finding.RuleId,
                    Title = finding.Title,
                    Description = finding.Description,
                    Recommendation = finding.Recommendation
                }).ToList(),

                PlanFindings = analysis.PlanFindings.Select(finding => new HistoryFinding
                {
                    RuleId = finding.RuleId,
                    Title = finding.Title,
                    Description = finding.Description,
                    Recommendation = finding.Recommendation,
                    NodeType = finding.Node.NodeType,
                    RelationName = finding.Node.RelationName
                }).ToList(),

                CombinedFindings = analysis.CombinedFindings.Select(finding => new HistoryFinding
                {
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