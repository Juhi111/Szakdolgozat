# Több javítás egyetlen SQL-javaslatban

Az összegző ablak a `SuggestCombinedStatement` metódust használja. Ez a kijelölt utasításból indul, alkalmaz egy támogatott átírást, majd a kapott SQL-en újra végigvizsgálja a szabályokat. Addig folytatja, amíg nincs további alkalmazható módosítás. Az azonos szabály több előfordulása és az átírás által lehetővé tett következő javítás is bekerül (például az IN-lista ismétlődő értékeinek törlése után az egyelemű IN egyszerűsítése).

A szabályregisztráció sorrendje dönti el a lépések sorrendjét. Ez egy determinisztikus javítási sorozat, nem az összes lehetséges alternatíva teljesítmény szerinti keresése. Az eredeti SQL és az eredeti terv nem változik; a közös javaslat külön mérhető.

Az egyedi szabályok és a `Suggest` továbbra is elérhetők. A közös javaslat `AppliedSteps` listájában minden lépés megtartja a bemeneti/kimeneti SQL-t, a szabályazonosítót, a magyarázatot és a metaadatfeltételeket. Egyetlen módosítás esetén az eredeti javaslattípus marad meg.

## Ellenőrzés

Futtatás előtt a teljes lépéssor folytonosságát és a végső SQL egyezését ellenőrizzük. A nem kapcsolati szabályok minden lépését újrageneráljuk, és az újra feloldott oszlop metaadatfeltételét használjuk. A kapcsolati átírásokat a meglévő friss katalógusos ellenőrzés igazolja újra. Ha bármelyik lépés ellenőrzése hibát jelez, a teljes javaslat futtatása tiltott. Az ismeretlen vagy kézi ellenőrzést igénylő feltételek nem válnak igazolttá attól, hogy több átírást összefűztünk.

Ismétlődő SQL-állapot vagy 256-nál több lépés esetén nem adunk ki részlegesen teljesnek nevezett javaslatot: a motor hibát jelez. Az összetett javaslatra vonatkozó típusellenőrzés nem bizonyít eredményazonosságot, például az aritmetikai túlcsordulást külön kell megvizsgálni.

## Mi maradhat változatlan?

Az antimintát felismerő szabály és az automatikus átíró szabály külön feladat. Nem minden jelzéshez van támogatott, jelentésmegőrző átírás. Ebben a fejlesztésben az átíró szabályok alkalmazhatósági körét nem bővítettük.

A hosszú customers példában ezért továbbra is csak az aritmetika írható át: az `upper(city) LIKE 'BUDAPEST'` nem közvetlen oszlopos LIKE; a date-cast `>=` és az évkinyerés `<=` nem a jelenlegi egyenlőségi dátumátírás; a `registered_at` timestamptz típusa sem támogatott ezekben az átírásokban. A wildcard, DISTINCT, összefűzés, CASE, hiányzó rendezés stb. nem távolítható el általánosan az eredmény megváltoztatása nélkül.

Az alkalmazás a kész javaslatot a meglévő formai elemzővel újraelemzi, és a felületen felsorolja a megmaradt megfigyeléseket. Az elvégzett módosítások és a megmaradó találatok a futtatási összehasonlítás PDF-jébe is bekerülnek.

## Felületi teszt

Az `examples/combined-rewrites/queries.sql` első SELECT-je hat egymásra épülő módosítást ad egyetlen javaslatban. A második a több HAVING-feltétel áthelyezését és az aritmetikát kombinálja. A `COUNT(*) > 1` összesítési feltétel a HAVING részben marad. Kérd le az eredeti tervet, nyisd meg az összegzést és az SQL-javaslatokat, majd ellenőrizd a végső scriptet és a módosítások listáját. A korábbi hosszú, több antimintás SELECT-nél a megmaradó találatok listája is ellenőrizhető.
