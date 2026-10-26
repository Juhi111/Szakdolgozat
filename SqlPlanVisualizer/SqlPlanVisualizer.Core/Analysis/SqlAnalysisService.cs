using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class SqlAnalysisService
    {
        private readonly List<ISqlAnalysisRule> _rules;

        /// <summary>
        /// Létrehozza az elemző szolgáltatást, és saját listában tárolja a megadott szabályokat.
        /// </summary>
        /// <param name="rules">Az elemzés során alkalmazandó szabályok.</param>
        /// <exception cref="ArgumentNullException">A szabálygyűjtemény null.</exception>
        public SqlAnalysisService(IEnumerable<ISqlAnalysisRule> rules)
        {
            if(rules is null)
                throw new ArgumentNullException(nameof(rules));
            _rules = rules.ToList();
        }

        /// <summary>
        /// Minden megadott utasításra alkalmazza az összes regisztrált szabályt,
        /// és közös listába gyűjti a találatokat.
        /// </summary>
        /// <param name="statements">Az elemzendő SQL-utasítások összefoglalói.</param>
        /// <returns>A szabályok által előállított találatok listája.</returns>
        /// <exception cref="ArgumentNullException">Az utasításgyűjtemény null.</exception>
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