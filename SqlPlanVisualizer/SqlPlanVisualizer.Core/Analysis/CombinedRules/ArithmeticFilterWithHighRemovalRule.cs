namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class ArithmeticFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public ArithmeticFilterWithHighRemovalRule() : base(
        "ARITHMETIC_ON_COLUMN", "ARITHMETIC_FILTER_WITH_HIGH_REMOVAL",
        "Aritmetikai szűrés sok eldobott sorral",
        "Vizsgáld meg az oszlopon végzett számítás elkerülhetőségét. Átrendezésnél az egészosztás, kerekítés, túlcsordulás és negatív értékek miatt külön ellenőrizd az eredmény azonosságát.") { }
}
