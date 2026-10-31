using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class PlanAnalysisService
    {
        private readonly List<IPlanAnalysisRule> _rules;

        public PlanAnalysisService(IEnumerable<IPlanAnalysisRule> rules)
        {
            ArgumentNullException.ThrowIfNull(rules);
            _rules = rules.ToList();
        }

        public List<PlanAnalysisFinding> Analyze(IEnumerable<PlanNode> roots)
        {
            ArgumentNullException.ThrowIfNull(roots);

            var findings = new List<PlanAnalysisFinding>();
            int planIndex = 1;

            foreach (var root in roots)
            {
                var stack = new Stack<PlanAnalysisContext>();
                stack.Push(new PlanAnalysisContext(root, planIndex));

                while (stack.Count > 0)
                {
                    var context = stack.Pop();
                    var node = context.Node;

                    foreach (var rule in _rules)
                    {
                        foreach (var finding in rule.Analyze(node, context))
                        {
                            finding.PlanIndex = context.PlanIndex;
                            findings.Add(finding);
                        }
                    }

                    for (int i = node.Children.Count - 1; i >= 0; i--)
                    {
                        var childContext = new PlanAnalysisContext(
                            node.Children[i], context.PlanIndex, context);

                        stack.Push(childContext);
                    }
                }

                planIndex++;
            }

            return findings;
        }
    }
}