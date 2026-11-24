namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>Egy külön ellenőrizendő SQL-változat; nem igazolja az eredmények azonosságát.</summary>
    public sealed record SqlRewriteSuggestion
    {
        public SqlPlanVisualizer.Core.Models.Database.RewriteMetadataRequirement? MetadataRequirement { get; init; }
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
        public string VerificationStatus => "Ellenőrizendő javaslat – az eredményazonosság és a gyorsulás még nincs igazolva.";
    }
}
