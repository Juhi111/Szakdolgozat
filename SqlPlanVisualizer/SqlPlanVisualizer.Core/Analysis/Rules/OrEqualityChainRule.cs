using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurrences;

namespace SqlPlanVisualizer.Core.Analysis.Rules;

public sealed class OrEqualityChainRule : ISqlAnalysisRule
{
    public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        return statement.PredicateSimplifications.Where(o => o.Kind == PredicateSimplificationKind.OrEqualityChain).Select(o => new SqlAnalysisFinding
        {
            RuleId = "OR_EQUALITY_CHAIN", StatementIndex = statement.StatementIndex, SourceOffset = o.SourceOffset,
            Title = "Azonos oszlopra írt OR-feltételek", Description = $"Az OR ágai ugyanazt az oszlopot hasonlítják különböző literálokhoz. Hely: {o.Location}.",
            Recommendation = "Az IN lista tömörebben fejezheti ki ezt a feltételt. Ez olvashatósági javaslat, önmagában nem bizonyít lassulást."
        }).ToList();
    }
}
