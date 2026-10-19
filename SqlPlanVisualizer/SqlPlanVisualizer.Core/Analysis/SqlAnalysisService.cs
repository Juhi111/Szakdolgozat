using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class SqlAnalysisService
    {
        private readonly List<ISqlAnalysisRule> _rules;

        public SqlAnalysisService(IEnumerable<ISqlAnalysisRule> rules)
        {
            if(rules is null)
                throw new ArgumentNullException(nameof(rules));
            _rules = rules.ToList();
        }

        public List<SqlAnalysisFinding> Analyze(IEnumerable<SqlStatementSummary> statements) {
            if(statements is null)
                throw new ArgumentNullException(nameof(statements));

            List<SqlAnalysisFinding> findings = new List<SqlAnalysisFinding>();
            
                foreach (var statement in statements) {
                    foreach (var rule in _rules) { 
                        findings.AddRange(rule.Analyze(statement));
                    }
                }            
            return findings;
        }
    }
}