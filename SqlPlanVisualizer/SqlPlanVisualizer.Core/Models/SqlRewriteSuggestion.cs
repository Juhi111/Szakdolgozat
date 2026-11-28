namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>Egy külön ellenőrizendő SQL-változat; nem igazolja az eredmények azonosságát.</summary>
    public sealed record SqlRewriteSuggestion
    {
        public IReadOnlyList<SqlAnalysisFinding>? RemainingFindings { get; init; }
        public bool HasRemainingAnalysis => RemainingFindings is not null;
        public string RemainingFindingsText => RemainingFindings is null ? string.Empty : RemainingFindings.Count == 0
            ? "A javasolt SQL-ben a jelenlegi formai szabályok nem jeleztek további találatot."
            : "Megmaradó megfigyelések – az alábbi részek változatlanok maradtak:\n" +
                string.Join("\n", RemainingFindings.GroupBy(f => f.RuleId).Select(g => $"• {g.First().Title} ({g.Count()}): {RemainingReason(g.Key)}"));
        private static string RemainingReason(string ruleId) => ruleId switch
        {
            "SELECT_STAR" => "A szükséges oszlopokat neked kell kiválasztanod.",
            "LIMIT_WITHOUT_ORDER" => "A kívánt sorrendet nem lehet automatikusan eldönteni.",
            "LEADING_WILDCARD_SEARCH" => "A kezdő helyettesítő karakter elhagyása megváltoztatná a keresés jelentését.",
            "CONCATENATION_IN_PREDICATE" => "Az összefűzött érték keresése nem cserélhető általánosan külön oszlopok keresésére.",
            "FUNCTION_ON_COLUMN" => "A függvény eredménye része a feltételnek; egyszerű elhagyása megváltoztathatná az eredményt.",
            "DISTINCT_USAGE" => "A DISTINCT elhagyásához ebben az alakban nem sikerült igazolni az egyediséget a kulcsadatokból.",
            _ => "Erre az alakra nincs igazolt automatikus átírás; a megfigyelés javaslatát külön vizsgáld meg."
        };
        public const string CombinedRuleId = "COMBINED_REWRITE";
        public IReadOnlyList<SqlRewriteSuggestion> AppliedSteps { get; init; } = Array.Empty<SqlRewriteSuggestion>();
        public string AppliedChanges => AppliedSteps.Count == 0 ? Title : string.Join("\n", AppliedSteps.Select((step, i) => $"{i + 1}. {step.Title}"));
        public SqlPlanVisualizer.Core.Models.Database.RewriteMetadataRequirement? MetadataRequirement { get; init; }
        public bool RequiresRelationalMetadata { get; init; }
        public required string RuleId { get; init; }
        public required string Title { get; init; }
        public required string OriginalSql { get; init; }
        public required string SuggestedSql { get; init; }
        public required string Explanation { get; init; }
        public required string Preconditions { get; init; }
        public string BriefExplanation => RuleId switch
        {
            CombinedRuleId => $"{AppliedSteps.Count} támogatott módosítást alkalmaztunk egymás után. Az át nem írható részek megmaradtak; az eredményt és a teljesítményt ellenőrizni kell.",
            "ARITHMETIC_EQUALITY_REWRITE" => "A számítást áthelyeztük az összehasonlítás másik oldalára, hogy közvetlenül az oszlopra lehessen szűrni.",
            "COUNT_EXISTENCE_REWRITE" => "A változat azt vizsgálja, van-e találat, ahelyett hogy darabszámot kérne.",
            "LIKE_LITERAL_REWRITE" => "A helyettesítő karakter nélküli keresést egyszerű összehasonlításra cseréltük.",
            "DATE_CAST_RANGE_REWRITE" => "A dátummá alakítás helyett közvetlen dátumhatárokkal szűrünk.",
            "YEAR_RANGE_REWRITE" => "Az év kiolvasása helyett közvetlen évhatárokkal szűrünk.",
            "HAVING_GROUP_KEY_REWRITE" => "A csoportosító oszlop feltételét a csoportosítás elé helyeztük.",
            "OR_TO_IN_REWRITE" => "Az azonos oszlopra vonatkozó összehasonlításokat egy áttekinthetőbb IN listába vontuk össze.",
            "SINGLETON_IN_REWRITE" => "Az egyetlen értéket tartalmazó felsorolást egyszerű összehasonlításra cseréltük.",
            "DUPLICATE_IN_REWRITE" => "Eltávolítottuk a felsorolásból az ismétlődő literálértékeket.",
            _ => Explanation
        };
        // Csak a felhasználó által még ellenőrizendő feltételek jelennek meg a felületen.
        public string UserCheck => AppliedSteps.Count > 0
            ? string.Join("\n", AppliedSteps.Select(s => s.UserCheck).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
            : RuleId switch
        {
            "ARITHMETIC_EQUALITY_REWRITE" => "Ellenőrizd, hogy az eredeti számítás a szélső értékeknél sem okoz túlcsordulást.",
            "LIKE_LITERAL_REWRITE" => "Ellenőrizd, hogy a szövegek összehasonlítása ugyanúgy kezeli a kis- és nagybetűket, illetve az ékezeteket.",
            "COUNT_EXISTENCE_REWRITE" => "Ellenőrizd, hogy a szűrés korábbi leállása nem fed el hibát vagy mellékhatást.",
            "HAVING_GROUP_KEY_REWRITE" => "Ellenőrizd, hogy a korábbi szűrés nem változtatja meg az összesítések hibakezelését vagy mellékhatásait.",
            "DATE_CAST_RANGE_REWRITE" or "YEAR_RANGE_REWRITE" => "Időzónás oszlopnál az eredeti és a javasolt lekérdezést azonos munkamenet-időzónával hasonlítsd össze.",
            "BOOLEAN_CASE_REWRITE" or "NOT_IN_EXISTS_REWRITE" or "LEFT_JOIN_ANTI_REWRITE" or "REDUNDANT_DISTINCT_REWRITE" or "DISTINCT_JOIN_EXISTS_REWRITE" or
            "OR_TO_IN_REWRITE" or "SINGLETON_IN_REWRITE" or "DUPLICATE_IN_REWRITE" => string.Empty,
            _ => Preconditions
        };
        public bool HasDetailedExplanation => !string.IsNullOrWhiteSpace(Explanation) && !string.Equals(BriefExplanation.Trim(), Explanation.Trim(), StringComparison.Ordinal);
        public string VerificationStatus => "Ellenőrizendő javaslat – az eredményazonosság és a gyorsulás még nincs igazolva.";
    }
}
