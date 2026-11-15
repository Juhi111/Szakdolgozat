namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class LeadingWildcardWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public LeadingWildcardWithHighRemovalRule() : base(
        "LEADING_WILDCARD_SEARCH", "LEADING_WILDCARD_WITH_HIGH_REMOVAL",
        "Helyettesítő karakterrel kezdődő keresés mellett sok sor kiesett",
        "Ellenőrizd, szükséges-e a helyettesítő karakter a keresési minta elején. Elhagyása megváltoztatja a találatokat. " +
            "Gyakori részszövegkeresésnél vizsgáld meg a pg_trgm alapú index lehetőségét; a hatás a mintától is függ. Az eredmény " +
            "azonosságát és a teljes lekérdezés futási idejét is ellenőrizd.") { }
}
