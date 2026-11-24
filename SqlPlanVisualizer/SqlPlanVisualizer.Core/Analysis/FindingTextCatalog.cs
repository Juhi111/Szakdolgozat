namespace SqlPlanVisualizer.Core.Analysis;

public sealed record FindingText(string Summary, string NextStep);

/// <summary>A felület és a PDF közös, rövid szövegei. A teljes szakmai indoklás külön megmarad.</summary>
public static class FindingTextCatalog
{
    private static readonly IReadOnlyDictionary<string, FindingText> Texts = new Dictionary<string, FindingText>
    {
        ["SELECT_STAR"] = new("A lekérdezés minden oszlopot elkér, akkor is, ha csak néhányra van szükség.", "Sorold fel azokat az oszlopokat, amelyeket valóban használsz."),
        ["DISTINCT_USAGE"] = new("A DISTINCT eltávolítja az ismétlődő eredménysorokat. Ez külön feldolgozást igényelhet.", "Ellenőrizd, szükség van-e az ismétlődések eltávolítására, és mi okozza őket."),
        ["NULL_COMPARISON"] = new("A NULL értéket hagyományos összehasonlítással vizsgálja a lekérdezés.", "NULL ellenőrzésére használj IS NULL vagy IS NOT NULL feltételt."),
        ["LIMIT_WITHOUT_ORDER"] = new("Nincs meghatározva, mely sorok kerüljenek a korlátozott eredménybe.", "Ha számít, mely sorokat kapod vissza, adj meg egyértelmű ORDER BY sorrendet."),
        ["LIKE_WITHOUT_WILDCARD"] = new("A LIKE minta nem tartalmaz helyettesítő karaktert.", "Ellenőrizd, hogy azonos eredménnyel helyettesíthető-e egyszerű egyenlőséggel."),
        ["LEADING_WILDCARD_SEARCH"] = new("A keresés a szöveg belsejében is kereshet. Ezt egy hagyományos index nem feltétlenül gyorsítja.", "Gyakori keresésnél vizsgáld meg a megfelelő szöveges index lehetőségét."),
        ["FUNCTION_ON_COLUMN"] = new("A keresés előtt egy függvény átalakítja az oszlop értékét.", "Nézd meg, van-e a keresési kifejezéshez illő index; az átalakítást csak az eredmény megőrzésével hagyd el."),
        ["CAST_ON_COLUMN"] = new("A szűrés más adattípusra alakítja az oszlopot.", "Vizsgáld meg, hogy az átalakítás helyett a keresett érték adható-e meg az oszlop típusában."),
        ["ARITHMETIC_ON_COLUMN"] = new("A szűrés az oszlopon végzett számítás eredményét vizsgálja.", "Ha a típus és a számítás engedi, alakítsd át közvetlen oszlop-összehasonlításra."),
        ["NOT_IN_SUBQUERY"] = new("A NOT IN eredményét az allekérdezésben előforduló NULL érték is befolyásolhatja.", "Ellenőrizd a NULL eseteket, mielőtt NOT EXISTS alakra írod át."),
        ["NOT_IN_WITH_NULL_LITERAL"] = new("A NOT IN felsorolásban NULL is szerepel, ezért a feltétel váratlanul kizárhat sorokat.", "A hiányzó értékek kezelését fogalmazd meg külön, egyértelmű feltétellel."),
        ["DATE_CAST_IN_PREDICATE"] = new("A lekérdezés dátummá alakítja az oszlopot a szűréshez.", "Nézd meg, megadható-e ugyanaz a feltétel kezdő és záró időponttal."),
        ["YEAR_EXTRACTION_IN_PREDICATE"] = new("A szűrés az oszlopból kiolvasott évet hasonlítja össze.", "Ellenőrizd, használható-e helyette egy év elejétől a következő év elejéig tartó időintervallum."),
        ["CONCATENATION_IN_PREDICATE"] = new("A keresés több érték összefűzött szövegét vizsgálja.", "Ellenőrizd, megadható-e a keresés külön oszlopfeltételekkel, azonos jelentéssel."),
        ["TEXT_CAST_IN_PREDICATE"] = new("Az oszlopot szöveggé alakítva hasonlítjuk össze.", "Lehetőleg az eredeti adattípusban szűrj, ha ezzel megmarad a keresés jelentése."),
        ["OPTIONAL_PARAMETER_FILTER"] = new("Ugyanaz a feltétel kapcsolja be vagy ki a paraméteres szűrést.", "Nézd meg külön a kitöltött és az üres paraméterrel készült terveket."),
        ["CASE_IN_PREDICATE"] = new("A paraméter dönti el, melyik oszlop alapján történik a szűrés.", "Hasonlítsd össze a külön keresési esetek terveit; eltérő lekérdezések egyszerűbbek lehetnek."),
        ["COUNT_FOR_EXISTENCE"] = new("A lekérdezés darabszámot használ annak eldöntésére, hogy van-e találat.", "Ha csak a létezés számít, ellenőrizd az EXISTS vagy NOT EXISTS változatot."),
        ["ROW_FILTER_IN_HAVING"] = new("A lekérdezés csoportosítás után szűr egy csoportosító oszlopra.", "Vizsgáld meg, átvihető-e ez a feltétel a WHERE részbe. A PostgreSQL ezt már megtehette."),
        ["POSSIBLE_CARTESIAN_PRODUCT"] = new("A táblák között nem találtunk egyértelmű kapcsolási feltételt.", "Ellenőrizd, szándékos-e minden sorpár összekapcsolása."),
        ["LARGE_OFFSET"] = new("A lapozás sok sort átugrik, mielőtt visszaadná az eredményt.", "Nagy oldalszámnál vizsgáld meg az utolsó látott kulcs alapján történő lapozást."),
        ["UNION_DEDUPLICATION"] = new("A UNION az ismétlődő sorok eltávolítását is elvégzi.", "Csak akkor válts UNION ALL-ra, ha az ismétlődő sorok megtartása megfelelő."),
        ["NATURAL_JOIN_USAGE"] = new("A kapcsolat oszlopait az azonos oszlopnevek határozzák meg.", "Add meg kifejezetten a kapcsolási feltételt, hogy egy sémaváltozás ne módosítsa észrevétlenül az eredményt."),
        ["RANDOM_ORDERING"] = new("Az eredményt véletlenszerű értékek alapján rendezi a lekérdezés.", "Nagy táblánál vizsgáld meg, elegendő-e más mintavételi módszer."),
        ["OR_EQUALITY_CHAIN"] = new("Ugyanazt az oszlopot több értékhez hasonlítja az OR feltétellánc.", "Az IN felsorolás olvashatóbb lehet; gyorsulást önmagában nem garantál."),
        ["SINGLETON_IN"] = new("Az IN felsorolás egyetlen értéket tartalmaz.", "Egyszerűsíthető közvetlen összehasonlításra, a NULL viselkedés megőrzésével."),
        ["DUPLICATE_IN_VALUES"] = new("Ugyanaz az érték többször szerepel a felsorolásban.", "Távolítsd el az azonos literálok ismétléseit az olvashatóság javítására."),
        ["ROW_ESTIMATE_MISMATCH"] = new("A tervben becsült és mért sorszám jelentősen eltér.", "Először ellenőrizd a LIMIT miatti korai leállást, majd a statisztikákat és a szűrés feltételeit."),
        ["SORT_SPILL"] = new("A rendezés lemezterületet is használt.", "Vizsgáld meg, csökkenthető-e a rendezés bemenete, és indokolt-e a memória célzott módosítása."),
        ["HASH_MULTIPLE_BATCHES"] = new("A kapcsoláshoz készített hash tábla több részletben dolgozott.", "Ellenőrizd a bemeneti adatmennyiséget, a becsléseket és a memóriaigényt."),
        ["HASH_AGGREGATE_SPILL"] = new("A csoportosításhoz lemezterület is kellett.", "Vizsgáld meg a csoportok számát és azt, csökkenthető-e a feldolgozandó adat."),
        ["HIGH_FILTER_REMOVAL"] = new("A művelet sok beolvasott sort kiszűrt.", "Nézd meg, szűkíthető-e korábban a beolvasás. A sok eldobott sor önmagában nem jelent lassú lekérdezést."),
        ["LOSSY_BITMAP_HEAP_SCAN"] = new("A bitmap beolvasás egyes lapokon újra ellenőrizte a sorokat.", "Ellenőrizd az újravizsgálatok költségét és a rendelkezésre álló memóriát."),
        ["HIGH_HEAP_FETCHES"] = new("Az indexből olvasás mellett sokszor a táblát is el kellett érni.", "Nézd meg a karbantartási adatokat és a láthatóság állapotát; ez nem feltétlenül hiányzó index jele."),
        ["REPEATED_SEQUENTIAL_SCAN"] = new("Ugyanaz a táblabeolvasás többször lefutott.", "Vizsgáld meg a kapcsolás módját és az ismételt beolvasások összesített költségét."),
        ["LARGE_SORT_FOR_LIMIT"] = new("Kevés kért eredménysorhoz sok adatot kapott a rendezés.", "Ellenőrizd, csökkenthető-e a rendezés bemenete, vagy segíthet-e megfelelő index."),
        ["PARALLEL_WORKER_SHORTAGE"] = new("A tervezettnél kevesebb párhuzamos dolgozó indult.", "Nézd meg a szerver terhelését és a párhuzamosítás korlátait."),
        ["INTERMEDIATE_STORAGE_SPILL"] = new("A köztes eredmény egy részét lemezen tárolta a művelet.", "Ellenőrizd, csökkenthető-e a köztes eredmény mérete, és mekkora a tényleges lemezműveleti költség."),
        ["ORDERED_LIMIT_WITH_LARGE_SORT"] = new("Kevés kért eredménysorhoz sok adatot kapott a rendezés. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Ellenőrizd, csökkenthető-e a rendezés bemenete, vagy segíthet-e megfelelő index."),
        ["LARGE_OFFSET_WITH_LARGE_SORT"] = new("A lapozás sok sort átugrik, mielőtt visszaadná az eredményt. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Nagy oldalszámnál vizsgáld meg az utolsó látott kulcs alapján történő lapozást."),
        ["ORDER_BY_WITH_SORT_SPILL"] = new("A rendezés lemezterületet is használt. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Vizsgáld meg, csökkenthető-e a rendezés bemenete, és indokolt-e a memória célzott módosítása."),
        ["RANDOM_ORDERING_WITH_SPILL"] = new("Az eredményt véletlenszerű értékek alapján rendezi a lekérdezés. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Nagy táblánál vizsgáld meg, elegendő-e más mintavételi módszer."),
        ["HAVING_FILTER_WITH_HIGH_REMOVAL"] = new("A lekérdezés csoportosítás után szűr egy csoportosító oszlopra. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Vizsgáld meg, átvihető-e ez a feltétel a WHERE részbe. A PostgreSQL ezt már megtehette."),
        ["FUNCTION_FILTER_WITH_HIGH_REMOVAL"] = new("A keresés előtt egy függvény átalakítja az oszlop értékét. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Nézd meg, van-e a keresési kifejezéshez illő index; az átalakítást csak az eredmény megőrzésével hagyd el."),
        ["LEADING_WILDCARD_WITH_HIGH_REMOVAL"] = new("A keresés a szöveg belsejében is kereshet. Ezt egy hagyományos index nem feltétlenül gyorsítja. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Gyakori keresésnél vizsgáld meg a megfelelő szöveges index lehetőségét."),
        ["LIKE_WITHOUT_WILDCARD_WITH_HIGH_REMOVAL"] = new("A LIKE minta nem tartalmaz helyettesítő karaktert. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Ellenőrizd, hogy azonos eredménnyel helyettesíthető-e egyszerű egyenlőséggel."),
        ["CAST_FILTER_WITH_HIGH_REMOVAL"] = new("A szűrés más adattípusra alakítja az oszlopot. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Vizsgáld meg, hogy az átalakítás helyett a keresett érték adható-e meg az oszlop típusában."),
        ["ARITHMETIC_FILTER_WITH_HIGH_REMOVAL"] = new("A szűrés az oszlopon végzett számítás eredményét vizsgálja. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Ha a típus és a számítás engedi, alakítsd át közvetlen oszlop-összehasonlításra."),
        ["YEAR_EXTRACTION_WITH_HIGH_REMOVAL"] = new("A szűrés az oszlopból kiolvasott évet hasonlítja össze. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Ellenőrizd, használható-e helyette egy év elejétől a következő év elejéig tartó időintervallum."),
        ["CONCATENATION_WITH_HIGH_REMOVAL"] = new("A keresés több érték összefűzött szövegét vizsgálja. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Ellenőrizd, megadható-e a keresés külön oszlopfeltételekkel, azonos jelentéssel."),
        ["OPTIONAL_PARAMETER_WITH_HIGH_REMOVAL"] = new("Ugyanaz a feltétel kapcsolja be vagy ki a paraméteres szűrést. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Nézd meg külön a kitöltött és az üres paraméterrel készült terveket."),
        ["CASE_FILTER_WITH_HIGH_REMOVAL"] = new("A paraméter dönti el, melyik oszlop alapján történik a szűrés. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Hasonlítsd össze a külön keresési esetek terveit; eltérő lekérdezések egyszerűbbek lehetnek."),
        ["DISTINCT_WITH_SPILL"] = new("A DISTINCT eltávolítja az ismétlődő eredménysorokat. Ez külön feldolgozást igényelhet. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Ellenőrizd, szükség van-e az ismétlődések eltávolítására, és mi okozza őket."),
        ["UNION_WITH_SPILL"] = new("A UNION az ismétlődő sorok eltávolítását is elvégzi. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Csak akkor válts UNION ALL-ra, ha az ismétlődő sorok megtartása megfelelő."),
        ["GROUP_BY_WITH_SPILL"] = new("A csoportosításhoz lemezterület is kellett. A kapcsolódó tervmegfigyelést is figyelembe vettük.", "Vizsgáld meg a csoportok számát és azt, csökkenthető-e a feldolgozandó adat."),
    };
    public static FindingText Get(string ruleId, string description, string recommendation) => Texts.TryGetValue(ruleId, out var text) ? text : new(description, recommendation);
    public static bool Contains(string ruleId) => Texts.ContainsKey(ruleId);
}
