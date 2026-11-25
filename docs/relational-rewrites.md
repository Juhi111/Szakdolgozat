# Relációs átírások és metaadatok az elemzésben

A meglévő kilenc család mellé négy új került. A felhasználói munkafolyamat változatlan: adatbázisból tervlekérés → elemzés → összegzés → SQL-javaslatok. Az eredeti SQL nem módosul. Az alternatívák lekérése/mérése továbbra is külön felhasználói művelet.

## Mit támogatunk?

| Szabályazonosító | Felismert helyzet | Szükséges feltétel |
|---|---|---|
| `NOT_IN_EXISTS_REWRITE` | teljes WHERE egy egyszerű NOT IN allekérdezés | mindkét oszlop igazoltan NOT NULL, azonos beépített egész/UUID típus, külön aliasok |
| `LEFT_JOIN_ANTI_REWRITE` | LEFT JOIN, utána jobb oldali oszlop IS NULL | a tesztelt jobb oldali oszlop NOT NULL, kimenet csak a bal táblából |
| `REDUNDANT_DISTINCT_REWRITE` | SELECT DISTINCT egy táblából | teljes, nem halasztható PK a kiválasztott oszlopok között |
| `DISTINCT_JOIN_EXISTS_REWRITE` | SELECT DISTINCT bal oldali oszlopokkal, INNER JOIN | teljes bal oldali PK a kimenetben, a jobb oldal csak létezésvizsgálatra szolgál |

Mind a négyhez explicit sémanév és táblaalias szükséges. A JOIN feltétele egyetlen, azonos beépített egész/UUID típusú oszlopok közötti egyenlőség. A DISTINCT-családok PK-indexének érvényes, kész, élő btree indexnek kell lennie, alapértelmezett operátorosztállyal. Összetett PK-nál minden kulcsoszlop szükséges.

Kimaradnak: CTE, halmazművelet, csoportosítás, ablak, ORDER BY, LIMIT/OFFSET, zárolás, wildcard/kifejezés a kimenetben, nézet, partíciós tábla, leszármazottakkal rendelkező tábla, sorbiztonság. A két DISTINCT-család ebben a változatban WHERE nélkül működik. NOT IN belső lekérdezése nem tartalmazhat szűrést. Virtuális generált oszlop és domain nem használható bizonyításhoz. A támogatott halmaz szándékosan szűk; a kihagyás nem jelenti, hogy egy lekérdezés nem javítható.

A nullázhatóság ismeretlen marad PostgreSQL 18-tól nem validált NOT NULL megszorítás mellett. A pillanatnyi NULL-arány nem bizonyít NOT NULL tulajdonságot.

## Metaadatok és futtatás

- Egy elemzéshez a tervben megnevezett táblák és az SQL explicit sémával azonosítható tábláinak metaadatait is begyűjtjük, legfeljebb 64 tábláig. Így az optimalizáló által eltávolított táblák sem maradnak ki.
- Az új javaslatok csak elérhető metaadatokkal jelennek meg. Fájlból betöltött terv önmagában nem bizonyítja a kapcsolatot a kiválasztott adatbázissal.
- Futtatás előtt frissen lekérjük a szükséges táblák metaadatait, és újrageneráljuk a javaslatot. Az azonosító és a generált SQL is meg kell egyezzen. Megváltozott, hiányzó vagy nem lekérhető feltétel blokkolja a futtatást.
- A metaadat-ellenőrzés és az EXPLAIN külön kapcsolat/tranzakció; egy közben végrehajtott sémamódosítást nem zárolunk ki. Az eredményazonosság továbbra is külön ellenőrzést igényel. A program nem készít indexet, nem hajt végre automatikus SQL-javítást.

## Megállapítások átírás nélkül

Az érintett tábla sorbecslési eltéréseihez statisztikai és karbantartási kontextus, sok eldobott sorhoz indexleltár, sok heap fetchhez látható-lap arány, ismételt táblabejáráshoz kapcsolási kontextus társul. Az értelmezés táblanév és séma szerint illeszkedik a találat csomópontjához; egy fölötte álló Sort eltérését nem tulajdonítjuk automatikusan az összes alatta lévő táblának.

A nem használható indexek, teljes kulcsok, nullázhatóság, valamint az egyszerű szűrésben érintett oszloptípusok külön magyarázatot kapnak. Nincs automatikus „készíts indexet” vagy „futtass VACUUM-ot” ítélet. A megállapítások a meglévő összegzés, PDF és előzménypillanatkép útvonalán jelennek meg.

## Kipróbálás

1. Saját tesztadatbázison, pgAdminból egyszer futtasd az `examples/relational-rewrites/setup.sql` fájlt.
2. Az alkalmazásban válaszd ezt az adatbázist; sémának megadható `rewrite_lab`.
3. Töltsd be az `examples/relational-rewrites/queries.sql` fájlt. Utasításonként kérj becsült vagy mért tervet, majd elemezz.
4. Az első négy utasításnál az összegzés SQL-javaslatai között megjelenik a megfelelő változat. Az ötödiknél nem szabad DISTINCT-elhagyást ajánlani. A hatodik a metaadatok átírás nélküli használatát mutatja.
5. Az újraellenőrzés próbájához az első javaslat elkészülte után külön pgAdmin-kapcsolaton futtasd: `ALTER TABLE rewrite_lab.orders ALTER COLUMN customer_id DROP NOT NULL;`. A korábbi javaslat futtatásának blokkolódnia kell. Ezután a tesztadatok mellett visszaállítható: `ALTER TABLE rewrite_lab.orders ALTER COLUMN customer_id SET NOT NULL;`.

## Ellenőrzés

Az egységtesztek a támogatott alakokat, összetett PK-t, NULL/ismeretlen adatokat, halasztható/hibás indexeket, aliasütközést és a futtatás előtti megváltozott feltételeket vizsgálják. Az élő PostgreSQL-teszt mindkét irányú `EXCEPT ALL` összehasonlítással ellenőrzi a tartalmat és a duplikátumokat: ismétlődő jobb oldali találatok, NULL-értékek, üres jobb oldal, idézett nevek és összetett kulcs esetén is.

Az élő teszt csak külön tesztadatbázisra mutató `SQLPLAN_METADATA_TEST_CONNECTION` környezeti változóval fut (egyedi nevű sémát hoz létre, végül törli). PostgreSQL 18+ alatt a nem validált NOT NULL esetet is ellenőrzi. A WPF-tesztekhez Windows szükséges.

Források: [NOT IN és EXISTS](https://www.postgresql.org/docs/18/functions-subquery.html), [öröklődés és kulcsok](https://www.postgresql.org/docs/18/ddl-inherit.html), [NOT NULL validálása](https://www.postgresql.org/docs/18/sql-altertable.html).
