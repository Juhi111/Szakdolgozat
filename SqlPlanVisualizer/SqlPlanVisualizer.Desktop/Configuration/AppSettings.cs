namespace SqlPlanVisualizer.Desktop.Configuration
{
    public class HighHeapFetchesSettings
    {
        public required double MinimumHeapFetches { get; init; }
    }

    public class RepeatedSequentialScanSettings
    {
        public required double MinimumLoops { get; init; }
        public required double MinimumExaminedRows { get; init; }
    }

    public class HighFilterRemovalSettings
    {
        public required double MinimumRemovedRows { get; init; }
        public required double RemovalRatioThreshold { get; init; }
    }
    
    public class AppSettings
    {
        public required PlanAnalysisSettings PlanAnalysis { get; init; }
        public required SqlAnalysisSettings SqlAnalysis { get; init; }        
    }

    public class LargeSortForLimitSettings
    {
        public required double MinimumInputRows { get; init; }
        public required double InputOutputRatioThreshold { get; init; }
    }

    public class PlanAnalysisSettings
    {
        public required RowEstimateMismatchSettings RowEstimateMismatch { get; init; }
        public required HighFilterRemovalSettings HighFilterRemoval { get; init; }
        public required HighHeapFetchesSettings HighHeapFetches { get; init; }
        public required RepeatedSequentialScanSettings RepeatedSequentialScan { get; init; }
        public required LargeSortForLimitSettings LargeSortForLimit { get; init; }
    }

    public class RowEstimateMismatchSettings
    {
        public required double RatioThreshold { get; init; }
        public required double DifferenceThreshold { get; init; }
    }

    public class SqlAnalysisSettings
    {
        public required long LargeOffsetThreshold { get; init; }
    }
}