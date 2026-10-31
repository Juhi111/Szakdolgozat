using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Analysis.PlanRules;
using SqlPlanVisualizer.Core.Analysis.Rules;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Desktop.Configuration;

namespace SqlPlanVisualizer.Desktop.Services
{
    /// <summary>
    /// Összeállítja az SQL- és tervelemző szolgáltatásokat a konfigurált szabályokkal.
    /// </summary>
    public static class AnalysisServiceFactory
    {
        /// <summary>
        /// Létrehozza az SQL-elemző szolgáltatást és regisztrálja a szabályait.
        /// </summary>
        public static SqlAnalysisService CreateSqlAnalysisService(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return new SqlAnalysisService(new ISqlAnalysisRule[]
            {
                new SelectStarRule(),
                new DistinctRule(),
                new NullComparisonRule(),
                new LimitWithoutOrderRule(),
                new LikeWithoutWildcardRule(),
                new LeadingWildcardSearchRule(),
                new FunctionOnColumnRule(),
                new CastOnColumnRule(),
                new ArithmeticOnColumnRule(),
                new NotInSubqueryRule(),
                new DateCastInPredicateRule(),
                new YearExtractionInPredicateRule(),
                new ConcatenationInPredicateRule(),
                new TextCastInPredicateRule(),
                new OptionalParameterFilterRule(),
                new CaseInPredicateRule(),
                new NotInWithNullLiteralRule(),
                new CountForExistenceRule(),
                new RowFilterInHavingRule(),
                new PossibleCartesianProductRule(),
                new LargeOffsetRule(threshold: settings.SqlAnalysis.LargeOffsetThreshold),
                new UnionDeduplicationRule(),
                new NaturalJoinUsageRule(),
                new RandomOrderingRule()
            });
        }

        /// <summary>
        /// Létrehozza a tervelemző szolgáltatást a konfigurált küszöbértékekkel.
        /// </summary>
        public static PlanAnalysisService CreatePlanAnalysisService(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var rowSettings = settings.PlanAnalysis.RowEstimateMismatch;
            var filterSettings = settings.PlanAnalysis.HighFilterRemoval;
            var heapSettings = settings.PlanAnalysis.HighHeapFetches;
            var repeatedScanSettings = settings.PlanAnalysis.RepeatedSequentialScan;
            var sortLimitSettings = settings.PlanAnalysis.LargeSortForLimit;

            return new PlanAnalysisService(new IPlanAnalysisRule[]
            {
                new RowEstimateMismatchRule(ratioThreshold: rowSettings.RatioThreshold, differenceThreshold: rowSettings.DifferenceThreshold),
                new SortSpillRule(),
                new HashBatchRule(),
                new HashAggregateSpillRule(),
                new HighFilterRemovalRule(minimumRemovedRows: filterSettings.MinimumRemovedRows, removalRatioThreshold: filterSettings.RemovalRatioThreshold),
                new LossyBitmapHeapScanRule(),
                new HighHeapFetchesRule(minimumHeapFetches: heapSettings.MinimumHeapFetches),
                new ParallelWorkerShortageRule(),
                new RepeatedSequentialScanRule(minimumLoops: repeatedScanSettings.MinimumLoops, minimumExaminedRows: repeatedScanSettings.MinimumExaminedRows),
                new LargeSortForLimitRule(minimumInputRows: sortLimitSettings.MinimumInputRows, inputOutputRatioThreshold: sortLimitSettings.InputOutputRatioThreshold),
                new IntermediateStorageSpillRule()
            });
        }
    }
}