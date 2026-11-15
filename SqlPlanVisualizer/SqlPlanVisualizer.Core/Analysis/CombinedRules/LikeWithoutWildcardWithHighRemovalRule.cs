namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public sealed class LikeWithoutWildcardWithHighRemovalRule : PredicateWithHighRemovalRule
{
    public LikeWithoutWildcardWithHighRemovalRule() : base(
        "LIKE_WITHOUT_WILDCARD", "LIKE_WITHOUT_WILDCARD_WITH_HIGH_REMOVAL",
        "Teljes szövegegyezés keresése mellett sok sor kiesett",
        "Ellenőrizd, pontos egyezés-e a cél, és milyen index segítheti azt. A LIKE mintájának escape-kezelése, az adattípus és " +
            "az összehasonlítási szabályok is számítanak; az ILIKE a kis- és nagybetűket is másként kezeli, mint egy egyszerű = " +
            "feltétel. Csak azonos jelentésű feltételre cserélj, és a gyorsulást mérd meg.") { }
}
