# SQL és EXPLAIN összekapcsolása

## Működés

A felhasználó megerősíti, melyik SQL-utasítás melyik EXPLAIN tervhez tartozik. Az alkalmazás ezt a megfelelést nem tudja a két fájlból önállóan bizonyítani. A kapcsolatok megfigyeléseket egyesítenek, nem oksági bizonyítékok és nem automatikus SQL-átírások.

A `CombinedRuleFactory` 15 szabályobjektumot regisztrál, amelyek összesen **17 fajta összekapcsolt megállapítást** készíthetnek. A DISTINCT/UNION/GROUP BY közös megvalósítást használ. A szűrések közös `PredicateWithHighRemovalRule` alaposztályt használnak; az egyes szabályokban csak az azonosítók és a saját szövegek szerepelnek.

### Szűrések

1. A `QueryBlockCollector` a felső SELECT szerkezetét és közvetlen táblahivatkozásait rögzíti.
2. A `PredicateCorrelationCollector` a meglévő collectorokat használja a WHERE/HAVING minták felismerésére. A felismerési feltételek nincsenek másodszor implementálva.
3. Az `ExpressionFingerprint` a támogatott részkifejezésekből helyfüggetlen AST-azonosítót készít. Megőrzi az operátorokat, paraméterszámokat, konstansokat és idézőjeles azonosítókat; az ismert táblaaliasokat feloldja.
4. A terv mapper a `Filter` mezőt is parse-olja. Csak a szövegliterálokon szereplő `::text` castot normalizálja, más castokat nem tüntet el. Szövegliterál tartalma nem válhat függvényhívássá.
5. A közös szabály azonos táblát, aliast, explicit séma esetén azonos sémát, azonos AST-részkifejezést és ugyanazon művelet `HIGH_FILTER_REMOVAL` találatát követeli meg.

A WHERE kapcsolatok egyetlen közvetlen táblára korlátozottak. A scan fölött Limit, Sort, Result, Gather és Gather Merge engedett egyetlen fő bemenettel. JOIN, CTE, allekérdezés vagy más lekérdezési szint feloldása nem történik találgatással. A HAVING kapcsolat csoportosító Aggregate csomóponthoz és ellenőrzött bemeneti táblához kötött.

A `Filter` nem minden esetben érvényes önálló SQL (például `SubPlan 1`). Sikertelen parse esetén a terv továbbra is betöltődik és önállóan elemezhető, csak az AST-alapú kapcsolat marad el.

## Támogatott kapcsolatok

| SQL/szerkezet | Tervbizonyíték | Összekapcsolt azonosító |
| --- | --- | --- |
| ORDER BY + LIMIT | Ugyanazon felső Limit: LARGE_SORT_FOR_LIMIT | ORDERED_LIMIT_WITH_LARGE_SORT |
| ORDER BY | Felső Sort: SORT_SPILL | ORDER_BY_WITH_SORT_SPILL |
| lower/upper az oszlopon | Egyező Filter, HIGH_FILTER_REMOVAL | FUNCTION_FILTER_WITH_HIGH_REMOVAL |
| Kezdő helyettesítő karakteres LIKE/ILIKE | Azonos oszlop, operátor és minta a Filterben | LEADING_WILDCARD_WITH_HIGH_REMOVAL |
| Helyettesítő karakter nélküli LIKE/ILIKE | Azonos oszlop, operátor és minta a Filterben | LIKE_WITHOUT_WILDCARD_WITH_HIGH_REMOVAL |
| Oszlopkonverzió (a date/text eseteket is beleértve) | Azonos cast a Filterben | CAST_FILTER_WITH_HIGH_REMOVAL |
| Oszlopon aritmetika | Azonos operátor, operandussorrend és konstans | ARITHMETIC_FILTER_WITH_HIGH_REMOVAL |
| EXTRACT(year)/date_part('year') | Azonos függvényes kifejezés | YEAR_EXTRACTION_WITH_HIGH_REMOVAL |
| Összefűzés | Azonos kifejezés, elválasztókkal együtt | CONCATENATION_WITH_HIGH_REMOVAL |
| Opcionális paraméter | Azonos OR-kifejezés és paraméterszám | OPTIONAL_PARAMETER_WITH_HIGH_REMOVAL |
| CASE alapú szűrés | Azonos CASE ágak és választóparaméter | CASE_FILTER_WITH_HIGH_REMOVAL |
| DISTINCT (DISTINCT ON is, ha a tervalak megfelelő) | Felső Unique→Sort vagy hash Aggregate lemezhasználata | DISTINCT_WITH_SPILL |
| UNION, ALL nélkül | Felső duplikációeltávolítás lemezhasználata | UNION_WITH_SPILL |
| GROUP BY, DISTINCT nélkül | Felső aggregálás vagy annak rendezése lemezhasználattal | GROUP_BY_WITH_SPILL |
| ORDER BY random() | Felső Sort Key random(), SORT_SPILL | RANDOM_ORDERING_WITH_SPILL |
| Nagy felső OFFSET | Azonos forráspozíciójú LARGE_OFFSET és felső Limit LARGE_SORT_FOR_LIMIT | LARGE_OFFSET_WITH_LARGE_SORT |
| Csoportkulcs szűrése HAVING-ben | Ugyanaz a Filter az Aggregate-en, HIGH_FILTER_REMOVAL | HAVING_FILTER_WITH_HIGH_REMOVAL |

A szűrési sorok mindegyikéhez HIGH_FILTER_REMOVAL is szükséges. A küszöböket a meglévő SQL-/tervszabályok alkalmazzák; az összekapcsolási szabályok nem vezetnek be új, párhuzamos küszöbértékeket. Egy szabály ugyanazon művelethez egy összefoglaló találatot ad, nem minden ismétlődő részkifejezéshez külön kártyát.

## Szándékosan nincs erőltetett párosítás

Az „összes összekapcsolás” nem a 24 SQL-szabály és 11 tervszabály összes kombinációját jelenti. A következő önálló találatokhoz a jelenlegi adatok nem adnak megbízható, általános kapcsolatot:

- SELECT * → sorok szélessége és ténylegesen szükséges oszlopok nélkül nem mondható meg, hogy ez okozta-e a spillt.
- NATURAL JOIN / lehetséges Descartes-szorzat → pontos join- és oszloperedet-feloldás szükséges.
- COUNT létezésvizsgálathoz / NOT IN allekérdezés → stabil SQL-allekérdezés–SubPlan megfeleltetés szükséges; a SubPlan sorszáma önmagában kevés.
- NULL-összehasonlítás / NOT IN NULL-literállal → elsősorban eredményhelyességi kérdések, nem egy tetszőleges lassulási jel magyarázatai.
- LIMIT ORDER BY nélkül → meghatározatlan sorrend, önmagában nincs hozzá specifikus lassulási bizonyíték.
- Hash-batch, heap fetch, lossy bitmap, workerhiány, köztes tárolás és általános sorszámbecslési eltérés → önálló tervmegállapítások maradnak, amíg nincs konkrét SQL-kifejezéshez igazolható kapcsolat.

Nem próbáljuk az optimalizáló által teljesen átírt kifejezéseket visszafejteni. Az implicit oszlopcastok, egyedi ESCAPE alakok, eltérően rendezett logikai ágak, összetettebb tervalakok vagy több tábla miatt egy helyes kapcsolat is kimaradhat. Ez konzervatív kihagyás, nem bizonyíték arra, hogy nincs probléma.

## Ellenőrzés és felületi próba

A `examples/combined-analysis` mappában 17 mesterséges SQL–JSON pár és az elvárt találatlisták szerepelnek. Nem mért tervek, és nem kell SQL-ként végrehajtani őket adatbázison.

Windows alatt a szokásos ellenőrzés:

```powershell
dotnet test SqlPlanVisualizer/SqlPlanVisualizer.Tests
```

A fejlesztési környezetben a standard VSTest futtató a processzinformációk hiánya miatt még a tesztek előtt hibára futott. A 107 xUnit Fact/Theory esetet ezért külön, soros futtató hajtotta végre a meglévő xUnit assertionökkel; mind sikeres. A 17 mintafájlpár teljes parse→tervelemzés→összekapcsolás lánca és pontos találatlistája szintén sikeres (összesen 124 ellenőrzés). A WPF felület tényleges kattintásos próbája Windows alatt végezhető el.

A WPF Desktop projekt Linuxon Windows-célzással is sikeresen lefordult (`EnableWindowsTargeting=true`). A meglévő `AppSettingsLoader` nullkezelési figyelmeztetése változatlan; ebben a feladatban nem módosítottuk.
