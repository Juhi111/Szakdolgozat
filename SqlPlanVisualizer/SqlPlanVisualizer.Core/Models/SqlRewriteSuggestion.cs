namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>Egy külön ellenőrizendő SQL-változat; nem igazolja az eredmények azonosságát.</summary>
    public sealed record SqlRewriteSuggestion
    {
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
            "ARITHMETIC_EQUALITY_REWRITE" => "A számítást áthelyeztük az összehasonlítás másik oldalára, hogy közvetlenül az oszlopra lehessen szűrni.",
            "COUNT_EXISTENCE_REWRITE" => "A változat azt vizsgálja, van-e találat, ahelyett hogy darabszámot kérne.",
            "LIKE_LITERAL_REWRITE" => "A helyettesítő karakter nélküli keresést egyszerű összehasonlításra cseréltük.",
            "DATE_CAST_RANGE_REWRITE" => "A dátummá alakítás helyett az adott nap kezdete és a következő nap kezdete közé szűrünk.",
            "YEAR_RANGE_REWRITE" => "Az év kiolvasása helyett egy teljes év időintervallumára szűrünk.",
            "HAVING_GROUP_KEY_REWRITE" => "A csoportosító oszlop feltételét a csoportosítás elé helyeztük.",
            "OR_TO_IN_REWRITE" => "Az azonos oszlopra vonatkozó összehasonlításokat egy áttekinthetőbb IN listába vontuk össze.",
            "SINGLETON_IN_REWRITE" => "Az egyetlen értéket tartalmazó felsorolást egyszerű összehasonlításra cseréltük.",
            "DUPLICATE_IN_REWRITE" => "Eltávolítottuk a felsorolásból az ismétlődő literálértékeket.",
            _ => Explanation
        };
        // Csak a felhasználó által még ellenőrizendő feltételek jelennek meg a felületen.
        public string UserCheck => RuleId switch
        {
            "ARITHMETIC_EQUALITY_REWRITE" => "Ellenőrizd, hogy az eredeti számítás a szélső értékeknél sem okoz túlcsordulást.",
            "LIKE_LITERAL_REWRITE" => "Ellenőrizd, hogy a szövegek összehasonlítása ugyanúgy kezeli a kis- és nagybetűket, illetve az ékezeteket.",
            "COUNT_EXISTENCE_REWRITE" => "Ellenőrizd, hogy a szűrés korábbi leállása nem fed el hibát vagy mellékhatást.",
            "HAVING_GROUP_KEY_REWRITE" => "Ellenőrizd, hogy a korábbi szűrés nem változtatja meg az összesítések hibakezelését vagy mellékhatásait.",
            "DATE_CAST_RANGE_REWRITE" or "YEAR_RANGE_REWRITE" => "Ezt a változatot csak date vagy időzóna nélküli timestamp oszlophoz használd.",
            "NOT_IN_EXISTS_REWRITE" or "LEFT_JOIN_ANTI_REWRITE" or "REDUNDANT_DISTINCT_REWRITE" or "DISTINCT_JOIN_EXISTS_REWRITE" or
            "OR_TO_IN_REWRITE" or "SINGLETON_IN_REWRITE" or "DUPLICATE_IN_REWRITE" => string.Empty,
            _ => Preconditions
        };
        public string VerificationStatus => "Ellenőrizendő javaslat – az eredményazonosság és a gyorsulás még nincs igazolva.";
    }
}
