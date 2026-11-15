namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class FunctionFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public FunctionFilterWithHighRemovalRule() : base(
        "FUNCTION_ON_COLUMN", "FUNCTION_FILTER_WITH_HIGH_REMOVAL",
        "Függvénnyel átalakított értékre keresés mellett sok sor kiesett",
        "Nézd meg a teljes szűrés feltételeit és a művelet idejét. Gyakori keresésnél vizsgáld meg a függvényes kifejezéshez " +
            "illeszkedő kifejezésindexet: ez az átalakított értékre épített index. A hatását a teljes lekérdezéssel mérd meg, mert " +
            "több feltétel együtt határozza meg a tervet.") { }
}
