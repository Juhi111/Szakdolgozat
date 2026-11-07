namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class ConcatenationWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public ConcatenationWithHighRemovalRule() : base(
        "CONCATENATION_IN_PREDICATE", "CONCATENATION_WITH_HIGH_REMOVAL",
        "Összefűzéses szűrés sok eldobott sorral",
        "Ellenőrizd, szükséges-e az összefűzött érték szerinti keresés. A külön oszlopokra bontás nem mindig azonos jelentésű; számít az elválasztó és a NULL-kezelés. Gyakori keresésnél vizsgáld meg a kifejezésindex lehetőségét.") { }
}
