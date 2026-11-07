namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class LikeWithoutWildcardWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public LikeWithoutWildcardWithHighRemovalRule() : base(
        "LIKE_WITHOUT_WILDCARD", "LIKE_WITHOUT_WILDCARD_WITH_HIGH_REMOVAL",
        "Helyettesítő karakter nélküli keresés sok eldobott sorral",
        "Ellenőrizd, hogy pontos egyezést szeretnél-e. LIKE esetén az escape-szabályok, ILIKE esetén a kis- és nagybetűk kezelése is számít; csak azonos jelentésű feltételre cserélj, és mérd meg az indexelés hatását.") { }
}
