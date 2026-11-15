namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class ConcatenationWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public ConcatenationWithHighRemovalRule() : base(
        "CONCATENATION_IN_PREDICATE", "CONCATENATION_WITH_HIGH_REMOVAL",
        "Összefűzött értékre keresés mellett sok sor kiesett",
        "Ellenőrizd, valóban az összefűzött értékre kell-e keresni. Külön oszlopok vizsgálata csak akkor megfelelő, ha az " +
            "elválasztók, mezőhatárok és NULL-értékek kezelése ugyanazt az eredményt adja. Gyakori keresésnél vizsgáld meg a teljes " +
            "összefűzésre készített kifejezésindex lehetőségét, majd mérd meg a teljes lekérdezést.") { }
}
