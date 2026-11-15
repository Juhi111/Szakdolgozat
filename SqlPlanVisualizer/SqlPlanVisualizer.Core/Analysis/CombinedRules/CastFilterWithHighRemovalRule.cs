namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class CastFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public CastFilterWithHighRemovalRule() : base(
        "CAST_ON_COLUMN", "CAST_FILTER_WITH_HIGH_REMOVAL",
        "Típusátalakítást tartalmazó szűrés mellett sok sor kiesett",
        "Ellenőrizd, megadható-e a keresett érték az oszlop eredeti típusának megfelelő paraméterként, az oszlop átalakítása " +
            "helyett. Dátumra keresésnél tartományos feltétel is szóba jöhet. Őrizd meg a konverzió, az időzóna és a hibakezelés " +
            "jelentését. Ha az átalakítás szükséges, vizsgáld meg a teljes kifejezés indexelhetőségét, és mérd meg a hatását.") { }
}
