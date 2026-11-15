namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class ArithmeticFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public ArithmeticFilterWithHighRemovalRule() : base(
        "ARITHMETIC_ON_COLUMN", "ARITHMETIC_FILTER_WITH_HIGH_REMOVAL",
        "Számított értékre szűrés mellett sok sor kiesett",
        "Ellenőrizd, elkerülhető-e az oszlopon végzett számítás úgy, hogy a feltétel eredménye azonos maradjon. Az egészosztás, " +
            "kerekítés, túlcsordulás, nullával osztás és negatív szorzó vagy osztó miatt ez külön vizsgálatot igényel. Ha a számítás " +
            "szükséges, gyakori keresésnél mérlegeld a teljes kifejezés indexelését, és mérd meg a hatását.") { }
}
