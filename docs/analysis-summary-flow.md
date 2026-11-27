# SQL-előkészítés és egységes összegzés

Az SQL-fájl megnyitása helyben feldolgozza az SQL-t, kitölti a SELECT-utasítások listáját és előállítja a formai találatokat. Nem futtat SQL-t adatbázison. A változatlan dokumentum eredménye újrahasznosul az Elemzés parancsban. Szerkesztés érvényteleníti a korábbi SQL-találatokat és párosítást; a következő Elemzés újra feldolgozza a dokumentumot.

Az adatbázisból lekért terv az eredeti SQL-hez kapcsolódik. Másik utasítás kiválasztásakor nem marad hozzárendelve a régi terv, és az elemzés nem vált vissza önkényesen a régi utasításra. Egyetlen importált terv szövegesen látszik; több importált tervnél megmarad a választó. Az importált terv összetartozását továbbra is a felhasználó erősíti meg.

## Megjelenítés

Az `AnalysisOverview` az összegző ablak és a PDF közös megjelenítési modellje. A szabályazonosító szerinti csoportosítás egy kártyán tartja az azonos témát, de a különböző SQL-előfordulások és tervcsomópontok részletes megfigyeléseit megtartja. A tervpozíció (például `1.2`) a gyökértől számított gyermekútvonal.

Az összekapcsoló szabályok megmaradnak belső bizonyítékkeresőként. Az explicit `RelatedSqlRuleId` alapján a kapcsolódó megfigyelés a megfelelő SQL-téma részleteihez kerül. Az önálló tervproblémák akkor is láthatók, ha nincs formai SQL-találat. A kapcsolódó tervtalálat azonosítása szabályazonosító és csomópontútvonal alapján történik, nem a rövidített szöveg alapján.

Az előzmények új, opcionális mezői megőrzik ezeket a kapcsolatokat. A régi előzmény továbbra is olvasható; bizonytalan szöveges egyezésnél nem távolítunk el találatot. Az adatbázis-metaadatok külön értékelése megmarad: pusztán hasonló szöveg alapján nem kapcsoljuk őket egy SQL-előforduláshoz.

## Felületi ellenőrzés Windows alatt

1. Nyiss meg két SELECT-et tartalmazó SQL-fájlt. Elemzés nélkül legyen két elem a listában, és jelenjenek meg a formai javaslatok.
2. Válaszd ki a második SELECT-et, majd kérj adatbázisos elemzést. Egyetlen gombnyomás elegendő; a lekért tervhez ne kelljen EXPLAIN-t választani.
3. Válaszd ki az első SELECT-et: ne maradjon hozzá párosítva a második terve. Új elemzés a kiválasztott utasításhoz kérjen tervet.
4. Importálj egy, majd több tervet tartalmazó JSON-t. Egy tervnél csak felirat, több tervnél választó legyen. Az összegzés előtt indítsd el az Elemzést.
5. Több csomóponton azonos típusú tervtalálat esetén egy kártya jelenjen meg, a részletekben minden csomóponttal és mérési adattal.
6. SQL–terv összekapcsolást adó példánál a tervmegfigyelés az SQL-javaslat részleteinél legyen; ne keletkezzen külön ismétlődő összekapcsolt lista.
7. LEFT JOIN → NOT EXISTS javaslatnál ne legyen a rövid magyarázattal azonos tartalmú lenyitható magyarázat. A ténylegesen bővebb magyarázatok maradjanak lenyithatók.
8. Ments PDF-et: ugyanazok a csoportosított megfigyelések, részletes adatok és feltételek jelenjenek meg.

A WPF-tesztek Windows futtatókörnyezetet igényelnek; a Linuxon végzett Desktop build önmagában nem helyettesíti a felületi ellenőrzést.
