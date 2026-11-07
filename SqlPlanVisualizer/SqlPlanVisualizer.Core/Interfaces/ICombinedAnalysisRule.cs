using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Interfaces
{
    public interface ICombinedAnalysisRule
    {
        IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis);
    }
}