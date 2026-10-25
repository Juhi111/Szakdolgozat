using SqlPlanVisualizer.Core.Models.Occurences;

namespace SqlPlanVisualizer.Core.Models
{
    public class SqlStatementSummary
    {
        public int StatementIndex { get; set; }
        public string StatementType { get; set; } = string.Empty;
        public bool HasWildcardProjection => WildcardProjections.Count > 0;
        public List<WildcardProjectionOccurrence> WildcardProjections { get; set; } = new();
        public List<DistinctOccurrence> DistinctOccurrences { get; } = new();
        public List<NullComparisonOccurrence> NullComparisons { get; } = new();
        public List<LimitOccurrence> LimitOccurrences { get; } = new();
        public List<LikeOccurrence> LikeOccurrences { get; } = new();
        public List<FunctionOnColumnOccurrence> FunctionOnColumnOccurrences { get; } = new();
        public List<CastOnColumnOccurrence> CastOnColumnOccurrences { get; } = new();
        public List<ArithmeticOnColumnOccurrence> ArithmeticOnColumnOccurrences { get; } = new();
        public List<NotInSubqueryOccurrence> NotInSubqueryOccurrences { get; } = new();
        public List<YearExtractionOccurrence> YearExtractionOccurrences { get; } = new();
        public List<ConcatenationOccurrence> ConcatenationOccurrences { get; } = new();
        public List<OptionalParameterFilterOccurrence> OptionalParameterFilters { get; } = new();
        public List<CaseInPredicateOccurrence> CaseInPredicateOccurrences { get; } = new();
        public List<NotInWithNullLiteralOccurrence> NotInWithNullLiterals { get; } = new();
        public List<CountForExistenceOccurrence> CountForExistenceOccurrences { get; } = new();
        public List<RowFilterInHavingOccurrence> RowFiltersInHaving { get; } = new();
        public List<PossibleCartesianProductOccurrence> PossibleCartesianProducts { get; } = new();
        public List<OffsetOccurrence> OffsetOccurrences { get; } = new();
        public List<UnionDeduplicationOccurrence> UnionDeduplications { get; } = new();
        public List<NaturalJoinOccurrence> NaturalJoins { get; } = new();
        public List<RandomOrderingOccurrence> RandomOrderings { get; } = new();
    }
}
