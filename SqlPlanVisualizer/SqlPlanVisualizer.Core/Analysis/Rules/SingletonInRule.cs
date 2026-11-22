using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurrences;

namespace SqlPlanVisualizer.Core.Analysis.Rules;

public sealed class SingletonInRule : ISqlAnalysisRule
{
    public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        return statement.PredicateSimplifications.Where(o => o.Kind == PredicateSimplificationKind.SingletonIn).Select(o => new SqlAnalysisFinding
        {
            RuleId = "SINGLETON_IN", StatementIndex = statement.StatementIndex, SourceOffset = o.SourceOffset,
            Title = "Egyelemű IN lista", Description = $"Az IN vagy NOT IN lista egyetlen nem NULL literált tartalmaz. Hely: {o.Location}.",
            Recommendation = "Beépített típusoknál egyszerű = vagy <> összehasonlítás használható. A terv akár változatlan is maradhat."
        }).ToList();
    }
}
