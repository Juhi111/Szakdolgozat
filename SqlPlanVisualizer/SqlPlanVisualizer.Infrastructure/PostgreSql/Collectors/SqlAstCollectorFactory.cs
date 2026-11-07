namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors
{
    /// <summary>
    /// Létrehozza az SQL feldolgozásához használt collectorokat.
    /// </summary>
    public static class SqlAstCollectorFactory
    {
        public static List<ISqlAstCollector> CreateDefault()
        {
            return new List<ISqlAstCollector>
            {
                new QueryBlockCollector(),
                new WildcardProjectionCollector(),
                new DistinctCollector(),
                new NullComparisonCollector(),
                new LimitCollector(),
                new LikeCollector(),
                new FunctionOnColumnCollector(),
                new CastOnColumnCollector(),
                new ArithmeticOnColumnCollector(),
                new NotInSubqueryCollector(),
                new YearExtractionCollector(),
                new ConcatenationCollector(),
                new OptionalParameterFilterCollector(),
                new CaseInPredicateCollector(),
                new NotInWithNullLiteralCollector(),
                new CountForExistenceCollector(),
                new RowFilterInHavingCollector(),
                new PossibleCartesianProductCollector(),
                new OffsetCollector(),
                new UnionDeduplicationCollector(),
                new NaturalJoinCollector(),
                new RandomOrderingCollector(),
                new PredicateCorrelationCollector()
            };
        }
    }
}