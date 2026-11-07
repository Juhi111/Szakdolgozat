using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    /// <summary>
    /// Összegyűjti a kiválasztott pár találatait, és futtatja az összegző szabályokat.
    /// </summary>
    public class CombinedAnalysisService
    {
        private readonly List<ICombinedAnalysisRule> _rules;

        public CombinedAnalysisService(IEnumerable<ICombinedAnalysisRule> rules)
        {
            ArgumentNullException.ThrowIfNull(rules);
            _rules = rules.ToList();
        }

        public CombinedAnalysisResult Analyze(
            StatementPlanPair pair,
            IEnumerable<SqlAnalysisFinding> sqlFindings,
            IEnumerable<PlanAnalysisFinding> planFindings)
        {
            ArgumentNullException.ThrowIfNull(pair);
            ArgumentNullException.ThrowIfNull(sqlFindings);
            ArgumentNullException.ThrowIfNull(planFindings);

            var matchingSqlFindings = sqlFindings
                .Where(finding => finding.StatementIndex == pair.Statement.StatementIndex);

            var matchingPlanFindings = planFindings
                .Where(finding => finding.PlanIndex == pair.Plan.PlanIndex);

            var result = new CombinedAnalysisResult(
                pair, matchingSqlFindings, matchingPlanFindings);

            foreach (var rule in _rules)
            {
                result.CombinedFindings.AddRange(rule.Analyze(result));
            }

            return result;
        }
    }
}