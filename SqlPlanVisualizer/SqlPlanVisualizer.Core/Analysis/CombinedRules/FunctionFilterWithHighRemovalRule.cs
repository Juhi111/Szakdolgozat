namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class FunctionFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public FunctionFilterWithHighRemovalRule() : base(
        "FUNCTION_ON_COLUMN", "FUNCTION_FILTER_WITH_HIGH_REMOVAL",
        "Függvényes szűrés sok eldobott sorral",
        "Vizsgáld meg a teljes szűrés szelektivitását és futási idejét. Gyakori lekérdezésnél mérd meg a függvényes kifejezéshez illeszkedő kifejezésindex hatását.") { }
}
