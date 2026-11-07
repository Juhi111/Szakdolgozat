namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class YearExtractionWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public YearExtractionWithHighRemovalRule() : base(
        "YEAR_EXTRACTION_IN_PREDICATE", "YEAR_EXTRACTION_WITH_HIGH_REMOVAL",
        "Évkivonásos szűrés sok eldobott sorral",
        "Vizsgáld meg, kifejezhető-e az évfeltétel az eredeti dátumoszlopra vonatkozó tartománnyal. Az időzónát, az összehasonlító operátort és a határértékeket is őrizd meg.") { }
}
