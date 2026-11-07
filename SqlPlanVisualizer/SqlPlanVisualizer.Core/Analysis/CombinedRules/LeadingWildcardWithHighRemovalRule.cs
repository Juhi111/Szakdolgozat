namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class LeadingWildcardWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public LeadingWildcardWithHighRemovalRule() : base(
        "LEADING_WILDCARD_SEARCH", "LEADING_WILDCARD_WITH_HIGH_REMOVAL",
        "Kezdő helyettesítő karakteres keresés sok eldobott sorral",
        "Vizsgáld meg, valóban részszöveges keresésre van-e szükség. Nagy adathalmaz és gyakori keresés esetén mérd meg egy megfelelő trigram index hatását; a keresés jelentését őrizd meg.") { }
}
