using SqlPlanVisualizer.Core.Interfaces;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

/// <summary>Az alapértelmezett összekapcsolások közös regisztrációja a felület és a tesztek számára.</summary>
public static class CombinedRuleFactory
{
    public static IReadOnlyList<ICombinedAnalysisRule> CreateDefault() => new ICombinedAnalysisRule[]
    {
        new OrderedLimitWithLargeSortRule(), new OrderByWithSortSpillRule(),
        new FunctionFilterWithHighRemovalRule(), new LeadingWildcardWithHighRemovalRule(),
        new LikeWithoutWildcardWithHighRemovalRule(), new CastFilterWithHighRemovalRule(),
        new ArithmeticFilterWithHighRemovalRule(), new YearExtractionWithHighRemovalRule(),
        new ConcatenationWithHighRemovalRule(), new OptionalParameterWithHighRemovalRule(),
        new CaseFilterWithHighRemovalRule(), new SetReductionWithSpillRule(),
        new RandomOrderingWithSpillRule(), new LargeOffsetWithLargeSortRule(),
        new HavingFilterWithHighRemovalRule()
    };
}
