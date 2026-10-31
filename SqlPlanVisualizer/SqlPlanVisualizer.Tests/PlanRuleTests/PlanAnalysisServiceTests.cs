using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using Xunit;

namespace SqlPlanVisualizer.Tests.PlanRuleTests
{
    public class PlanAnalysisServiceTests
    {
        [Fact]
        public void Analyze_PassesParentContextToRule()
        {
            var child = new PlanNode { NodeType = "Seq Scan" };
            var root = new PlanNode { NodeType = "Limit" };
            root.Children.Add(child);

            var rule = new ContextRecordingRule();
            var service = new PlanAnalysisService(new IPlanAnalysisRule[] { rule });

            service.Analyze(new[] { root });

            Assert.Equal(2, rule.Contexts.Count);

            var rootContext = rule.Contexts[0];
            var childContext = rule.Contexts[1];

            Assert.Null(rootContext.Parent);
            Assert.Same(root, rootContext.Node);
            Assert.Same(child, childContext.Node);
            Assert.Same(rootContext, childContext.Parent);
            Assert.Equal(1, childContext.PlanIndex);
        }

        private class ContextRecordingRule : IPlanAnalysisRule
        {
            public List<PlanAnalysisContext> Contexts { get; } = new();

            public List<PlanAnalysisFinding> Analyze(PlanNode node, PlanAnalysisContext? context = null)
            {
                ArgumentNullException.ThrowIfNull(context);
                Contexts.Add(context);

                return new List<PlanAnalysisFinding>();
            }
        }
    }
}
