using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Interfaces
{
    public interface IPlanAnalysisRule
    {
        List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null);
    }
}