# Automatikus elemzés és olvasható összegzés

## Felhasználói folyamat

A főablak tetején válassz módot, majd kattints az **Elemzés indítása** gombra:

- **Betöltött SQL / EXPLAIN fájlok:** nincs automatikus SQL-végrehajtás. Meglévő kapcsolat sem indít futtatást. A fájlos terv adatbázisa ismeretlen; ehhez nem rendelünk automatikusan katalógusadatokat.
- **Adatbázis – becsült terv:** a program lekéri az EXPLAIN tervet, elemzi az SQL-t és a tervet, lekéri a szükséges adatbázisadatokat, majd elkészíti az összegzést.
- **Adatbázis – SELECT futtatása és mérése:** EXPLAIN ANALYZE fut, tehát a SELECT ténylegesen végrehajtódik. Ez tudatosan választandó mód, nem alapértelmezés.

Egyetlen, igazoltan összetartozó SQL és adatbázisból lekért terv esetén az összegzés automatikusan megnyílik. Több utasítás esetén előbb válaszd ki az elemzendő utasítást; fájlból betöltött tervnél a párosítás továbbra is kézi. A régi, csak tervet betöltő menüpontok megmaradtak haladó használatra; a normál útvonalhoz nem szükségesek.

A **Megszakítás** gomb megszakítja a folyamatban lévő adatbázis-lekérést. A folyamat kétperces összesített időkeretet használ, a lekérések saját időkorlátai mellett. Új bemenet betöltése közben a régi eredményt nem kapcsoljuk az új SQL-hez.

## Automatikusan lekért adatok

| Adat | Kapcsolódó vizsgálatok |
|---|---|
| Oszloptípus, domain jelzés, nullázhatóság, kolláció, generált/identity jelzés | Típusfüggő átírások, dátum/év/szöveg/számítás, NULL-kockázatok |
| Indexek, kifejezések, kulcsok, INCLUDE, részleges feltétel, érvényesség, NULL-egyediség | Függvényes szűrés, rendezés, indexjavaslatok |
| PK, UNIQUE, FK és CHECK definíciók, validáltság és halaszthatóság | Egyediség és NULL feltételek ellenőrzésének háttere |
| Relációméret, becsült sorszám és lapadatok | Beolvasás, sorbecslés és táblához visszaolvasás |
| Élő/elhalt sorok becslése, módosítások, ANALYZE/VACUUM és automatikus változataik időpontja | Becslési eltérés, heap fetches, karbantartási javaslatok |
| Oszloponként null_frac, n_distinct, avg_width, correlation és inherited jelzés | Szelektivitás és sorbecslés részletes ellenőrzése |
| Statisztikai célérték, kiterjesztett statisztikák definíciói | Statisztikai lefedettség ellenőrzése |
| work_mem, hash_mem_multiplier, párhuzamos dolgozók korlátai, költségparaméterek, szerververzió, időzóna, DateStyle, statisztikagyűjtési kapcsolók | Lemezre kerülő műveletek, több hash köteg, lossy bitmap, párhuzamosítás és típusfeltételek háttere |
| Telepített bővítmények | Például pg_trgm elérhetősége részszöveges kereséshez |

A szabályoknak szükséges katalógusadatok egy közös szolgáltatásból érkeznek: párosított elemzésenként egy kapcsolat és read-only, repeatable-read tranzakció, legfeljebb 64 táblahivatkozás. Az oszlopadatok visszaolvasása a mentett pillanatképből történik, nem szabályonkénti adatbázis-kapcsolódással. A forrást és a lekérés idejét megőrizzük.

Nem kérünk le tényleges üzleti sorokat, leggyakoribb oszlopértékeket vagy hisztogramhatárokat. A lekérés nem módosít indexet, sémát, szerverbeállítást és nem futtat ANALYZE/VACUUM parancsot.

## Hogyan használjuk fel?

A típusfeltételek automatikusan befolyásolják az SQL-javaslat futtathatóságát. A megismert, nem támogatott típus blokkolja az adott átírás tervlekérését. Hiányzó vagy ismeretlen típusadat nem számít sikeres igazolásnak. A pillanatkép nem bizonyít eredményazonosságot.

A memória-, statisztikai, karbantartási és párhuzamosítási adatok a megfelelő meglévő találatokhoz adnak kontextust. Egy FK, UNIQUE vagy NOT NULL definícióból **nem** következtetünk automatikusan összetett JOIN/alkérdezés eredményének egyediségére vagy nullamentességére. Az FK/CHECK és oszlopstatisztikák részletesen megtekinthetők, de nem adnak új, bizonyítatlan átírási engedélyt.

A beállítások a metaadat-kapcsolat értékei; a korábbi terv készítésekor más értékek is érvényben lehettek. A számlálók becslések, a hiányzó időpontok nem bizonyítják a karbantartás hiányát. A negatív n_distinct arányt jelent, nem negatív darabszámot.

Ha egy tábla nem érhető el, a többi lekérése folytatódhat. Teljes kapcsolati hiba esetén az SQL- és tervelemzés megmarad, és az összegzés jelzi a hiányzó adatokat. Másik kapcsolat pillanatképét nem használjuk fel.

## Szövegezés és megjelenítés

Az összes jelenlegi SQL-, terv- és kombinált szabály központi rövid magyarázatot és következő lépést kapott. A felület először ezt mutatja; a teljes korábbi szakmai indoklás, helyadatok és alkalmazási feltételek lenyitható részletekben maradnak elérhetők. Az átírásokhoz is tartozik rövid magyarázat; alkalmazási feltételeik továbbra is láthatók.

A közös áttekintés összevonja az azonos rövid megállapításokat, és a kombinált találatban pontosan szereplő tervmegfigyelést nem sorolja fel még egyszer. Az eredeti találatlistákat megőrzi, ezért eltérő csomópontok bizonyítékai visszakereshetők.

A **Felhasznált adatbázisadatok…** ablak részletező nézet, nem kötelező munkafázis. Az adatok elemzés után már benne vannak; opcionálisan frissíthetők. A fő eredménybe, az automatikusan mentett új előzménybe és a PDF-be ugyanaz a lekért pillanatkép kerül. A későbbi kézi frissítés nem írja át a már mentett előzményt; új PDF-export a frissített állapotot mutatja.

## Ellenőrzés

- PostgreSQL 17-en integrációs teszt ellenőrzi a valódi katalóguslekérést, indexeket, INCLUDE/részleges indexet, FK/CHECK megszorításokat, oszlopstatisztikákat, beállításokat és a korlátozott jogosultságot.
- Az integrációs teszthez `SQLPLAN_METADATA_TEST_CONNECTION` változóban **külön tesztadatbázist** adj meg. A teszt saját, véletlen nevű sémákat és ideiglenes tesztszerepet hoz létre, majd töröl. Adminisztratív tesztjogosultság szükséges. A változó nélkül ezek a tesztek kihagyottként jelennek meg.
- A WPF-tesztek a módválasztás, hibás tervlekérés, fájlos mód és bemenetcsere kezelését is ellenőrzik; Windows alatt futtathatók.

Felületi próba: a szakdoga_lab kapcsolat mellett töltsd be a `examples/index-metadata/queries.sql` egyik utasítását. Válaszd a becsült adatbázisos módot, majd csak az Elemzés indítása gombot használd. Egy SQL esetén nyíljon meg a kész összegzés, a metaadatnézet pedig külön lekérés nélkül tartalmazza az adatokat. Ellenőrizd az SQL-javaslatok típusfeltételeit és a PDF rövid összegzését, majd fájlos módban azt, hogy nem indul adatbázisos SQL-végrehajtás.
