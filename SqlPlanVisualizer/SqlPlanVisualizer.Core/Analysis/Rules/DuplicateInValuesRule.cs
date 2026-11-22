using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurrences;

namespace SqlPlanVisualizer.Core.Analysis.Rules;

public sealed class DuplicateInValuesRule : ISqlAnalysisRule
{
    public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        return statement.PredicateSimplifications.Where(o => o.Kind == PredicateSimplificationKind.DuplicateInValues).Select(o => new SqlAnalysisFinding
        {
            RuleId = "DUPLICATE_IN_VALUES", StatementIndex = statement.StatementIndex, SourceOffset = o.SourceOffset,
            Title = "Ismétlődő értékek az IN listában", Description = $"Az IN vagy NOT IN listában azonos literál többször szerepel. Hely: {o.Location}.",
            Recommendation = "Hagyd meg egyszer az ismétlődő értékeket. Ettől áttekinthetőbb lesz a feltétel; gyorsulást csak mérés igazolhat."
        }).ToList();
    }
}
