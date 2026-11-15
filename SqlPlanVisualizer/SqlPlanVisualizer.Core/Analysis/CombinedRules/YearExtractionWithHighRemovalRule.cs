namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class YearExtractionWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public YearExtractionWithHighRemovalRule() : base(
        "YEAR_EXTRACTION_IN_PREDICATE", "YEAR_EXTRACTION_WITH_HIGH_REMOVAL",
        "Év szerinti szűrés mellett sok sor kiesett",
        "Dátum vagy időbélyeg esetén vizsgáld meg, kifejezhető-e a feltétel az eredeti oszlopra megadott tartománnyal. Vedd " +
            "figyelembe az időzónát, az összehasonlítás irányát és a határértékeket. Intervallumértékre ez nem általános " +
            "helyettesítő. Az eredmény azonosságát és az indexhasználat, illetve a futási idő változását is ellenőrizd.") { }
}
