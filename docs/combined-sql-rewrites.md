# Több javítás egyetlen SQL-javaslatban

Az összegző ablak a `SuggestCombinedStatement` metódust használja. Ez a kijelölt utasításból indul, alkalmaz egy támogatott átírást, majd a kapott SQL-en újra végigvizsgálja a szabályokat. Addig folytatja, amíg nincs további alkalmazható módosítás. Az azonos szabály több előfordulása és az átírás által lehetővé tett következő javítás is bekerül (például az IN-lista ismétlődő értékeinek törlése után az egyelemű IN egyszerűsítése).

A szabályregisztráció sorrendje dönti el a lépések sorrendjét. Ez egy determinisztikus javítási sorozat, nem az összes lehetséges alternatíva teljesítmény szerinti keresése. Az eredeti SQL és az eredeti terv nem változik; a közös javaslat külön mérhető.

Az egyedi szabályok és a `Suggest` továbbra is elérhetők. A közös javaslat `AppliedSteps` listájában minden lépés megtartja a bemeneti/kimeneti SQL-t, a szabályazonosítót, a magyarázatot és a metaadatfeltételeket. Egyetlen módosítás esetén az eredeti javaslattípus marad meg.

## Ellenőrzés

Futtatás előtt a teljes lépéssor folytonosságát és a végső SQL egyezését ellenőrizzük. A nem kapcsolati szabályok minden lépését újrageneráljuk, és az újra feloldott oszlop metaadatfeltételét használjuk. A kapcsolati átírásokat a meglévő friss katalógusos ellenőrzés igazolja újra. Ha bármelyik lépés ellenőrzése hibát jelez, a teljes javaslat futtatása tiltott. Az ismeretlen vagy kézi ellenőrzést igénylő feltételek nem válnak igazolttá attól, hogy több átírást összefűztünk.

Ismétlődő SQL-állapot vagy 256-nál több lépés esetén nem adunk ki részlegesen teljesnek nevezett javaslatot: a motor hibát jelez. Az összetett javaslatra vonatkozó típusellenőrzés nem bizonyít eredményazonosságot, például az aritmetikai túlcsordulást külön kell megvizsgálni.

## Mi maradhat változatlan?

Az antimintát felismerő szabály és az automatikus átíró szabály külön feladat. Nem minden jelzéshez létezik jelentésmegőrző átírás: a szükséges kimeneti oszlopokat vagy a kívánt rendezést nem választjuk ki a felhasználó helyett, a keresési mintákból pedig nem töröljük a wildcardokat.

A feltöltött hosszú customers SELECT már öt módosítást kap metaadatok nélkül: aritmetika, upper(city) LIKE egyszerűsítése, logikai CASE, dátumhatár és évhatár. Ha a katalógus igazolja a teljes elsődleges kulcs jelenlétét és a többi feltételt, a DISTINCT elhagyása a hatodik módosítás. Ez nem garantálja, hogy rendezés nélküli LIMIT esetén ugyanazt a részhalmazt választja a szerver.

## Összetett lekérdezések

A WHERE- és JOIN ON-feltételeket SELECT-enként vizsgáljuk; az allekérdezések belseje is bejárható. Az oszlopot mindig az adott SELECT saját, közvetlen tábláihoz rendeljük. Több táblánál a nem minősített, illetve a külső SELECT-re korreláló típusfüggő hivatkozásokhoz nem találgatunk táblát. CTE-s dokumentumoknál az átírás továbbra is kimarad; a CTE-névfeloldás külön feladat. A predikátumok AND alatti bejárása megmaradt, általános OR/NOT normalizálást nem végzünk. A HAVING és a kulcsokra épülő átírások a támogatott belső SELECT-ekben is alkalmazhatók, saját további korlátaikkal.

Új/bővített alakok:
- `upper/lower(oszlop) LIKE/NOT LIKE 'literál'`: a függvény megmarad, a wildcard nélküli operátor egyszerűsödik. Kollációs viselkedés külön ellenőrizendő.
- `CASE WHEN logikai_oszlop THEN 1 ELSE 0 END = 1`: `IS TRUE`, a NULL-kezelés megtartásával.
- Date-cast és évkinyerés: `=`, `<`, `<=`, `>`, `>=`, fordított operandusokkal is. Beépített date/timestamp/timestamptz típusok; DATE-határok, timestamptz esetén a munkamenet időzónája. Történeti időzóna-ugrások külön ellenőrizendők.
- Egész literálos összeadás/kivonás: egész és pontos numeric oszlopok; lebegőpontos típusok továbbra sem támogatottak.
- Redundáns DISTINCT: WHERE, rendezés és LIMIT/OFFSET mellett, `*`/`alias.*` projekcióval is, a katalógus alapján bizonyított kulccsal. A többi kapcsolati szabály korlátai nem lazultak.

Az alkalmazás a kész javaslatot a meglévő formai elemzővel újraelemzi, és a felületen felsorolja a megmaradt megfigyeléseket. Az elvégzett módosítások és a megmaradó találatok a futtatási összehasonlítás PDF-jébe is bekerülnek.

## Felületi teszt

Az `examples/combined-rewrites/queries.sql` első SELECT-je hat egymásra épülő módosítást ad egyetlen javaslatban. A második a több HAVING-feltétel áthelyezését és az aritmetikát kombinálja. A `COUNT(*) > 1` összesítési feltétel a HAVING részben marad. Kérd le az eredeti tervet, nyisd meg az összegzést és az SQL-javaslatokat, majd ellenőrizd a végső scriptet és a módosítások listáját. A korábbi hosszú, több antimintás SELECT-nél a megmaradó találatok listája is ellenőrizhető.

## Regressziós ellenőrzés

A felhasználó négy eredeti lekérdezése: `SqlPlanVisualizer/SqlPlanVisualizer.Tests/TestData/complex-rewrites.sql`.
`ComplexSqlRewriteTests` ellenőrzi az együttes átírásokat, a SELECT-hatóköröket és a kihagyandó eseteket.
`ComplexSqlRewriteLiveTests` a `SQLPLAN_METADATA_TEST_CONNECTION` beállításával külön tesztadatbázison fut: saját ideiglenes nevű sémát hoz létre és végül törli. Kétirányú EXCEPT ALL hasonlítja a sorokat és azok többszörösségét. A random() kimeneti értékét nem hasonlítja, a többi értéket és a darabszámot igen. A dátumoperátorokat UTC, Europe/Budapest és America/New_York időzónában, szökőnappal, NULL-okkal és végtelenekkel ellenőrzi. Ez a tesztadatokon igazolja az egyezést, nem az összes lehetséges lekérdezésre.
