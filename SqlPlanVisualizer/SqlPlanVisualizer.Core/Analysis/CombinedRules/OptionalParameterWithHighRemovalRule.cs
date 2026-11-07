namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class OptionalParameterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public OptionalParameterWithHighRemovalRule() : base(
        "OPTIONAL_PARAMETER_FILTER", "OPTIONAL_PARAMETER_WITH_HIGH_REMOVAL",
        "Opcionális paraméteres szűrés sok eldobott sorral",
        "Mérd meg külön a NULL és nem NULL paraméteres futásokat. Szükség esetén külön lekérdezési változatokkal vizsgáld a tervet; ez az egy mérés nem bizonyít általános tervhibát.") { }
}
