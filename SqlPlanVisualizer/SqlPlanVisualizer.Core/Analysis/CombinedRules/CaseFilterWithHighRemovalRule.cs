namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class CaseFilterWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public CaseFilterWithHighRemovalRule() : base(
        "CASE_IN_PREDICATE", "CASE_FILTER_WITH_HIGH_REMOVAL",
        "CASE alapú szűrés sok eldobott sorral",
        "Vizsgáld meg külön a választóparaméter ágait. Azonos jelentésű lekérdezési változatokkal ellenőrizd, javul-e az indexhasználat és a futási idő; őrizd meg a NULL-kezelést.") { }
}
