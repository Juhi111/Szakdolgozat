namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class CaseFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public CaseFilterWithHighRemovalRule() : base(
        "CASE_IN_PREDICATE", "CASE_FILTER_WITH_HIGH_REMOVAL",
        "CASE által választott oszlopra szűrés mellett sok sor kiesett",
        "Próbáld ki a CASE mindkét ágát jellemző paraméterekkel. Ha az egyik keresés lassú, vizsgáld meg két külön, " +
            "paraméterezett lekérdezés lehetőségét. A NULL-kezelés és a választási feltétel maradjon azonos. A változtatást a teljes " +
            "lekérdezés futási idejével ellenőrizd.") { }
}
