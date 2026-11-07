namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class CastFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public CastFilterWithHighRemovalRule() : base(
        "CAST_ON_COLUMN", "CAST_FILTER_WITH_HIGH_REMOVAL",
        "Oszlopkonverzió sok eldobott sorral",
        "Ellenőrizd, megőrizhető-e a feltétel jelentése az oszlop konvertálása nélkül, például megfelelő típusú paraméterrel vagy dátumtartománnyal. Vedd figyelembe az időzónát és a konverzió szabályait. Ha a kifejezés szükséges, vizsgáld meg az indexelhetőségét.") { }
}
