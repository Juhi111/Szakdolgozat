namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class OptionalParameterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public OptionalParameterWithHighRemovalRule() : base(
        "OPTIONAL_PARAMETER_FILTER", "OPTIONAL_PARAMETER_WITH_HIGH_REMOVAL",
        "Opcionális paraméteres szűrés mellett sok sor kiesett",
        "Mérd meg a NULL és a jellemző nem NULL paraméterekkel futó lekérdezéseket is, az alkalmazás tényleges " +
            "paraméterezésével. Ha a közös terv valamelyik esetben lassú, vizsgáld meg a szűrt és szűrés nélküli változat " +
            "különválasztását. Egyetlen futás alapján nem állapítható meg, hogy az általános, több paraméterértékhez használt terv " +
            "hibás.") { }
}
