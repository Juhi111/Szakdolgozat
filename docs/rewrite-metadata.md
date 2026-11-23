# SQL-átírások metaadat-ellenőrzése

A `feature/rewrite-metadata` az oszlop típusát és deklarált nullázhatóságát olvassa ki a kiválasztott PostgreSQL-kapcsolaton. Nem vizsgálja az adatsorokat, nem hoz létre indexet és nem bizonyít eredményazonosságot.

## Használat

1. Kérd le az eredeti SQL tervét az alkalmazásból, majd elemezd és nyisd meg az SQL-javaslatok ablakát.
2. A **Futtatás és összehasonlítás** fülön válassz alternatívát.
3. A **Típusfeltétel ellenőrzése / frissítése** gomb csak metaadatokat kér le; nem futtatja a vizsgált SQL-t.
4. Megjelenik az oszlop, típus, NULL-megengedés, az ellenőrzés ideje és forrása.
5. Ismerten nem megfelelő beépített típusnál a két tervlekérő gomb letiltódik. Friss ellenőrzés vagy másik javaslat választása után újraértékeljük a helyzetet.
6. Minden új tervlekérés friss metaadatot kér; a külön ellenőrző gomb nem kötelező előlépés. Az alkalmazás a szolgáltatásban is elutasítja a nem megfelelő típust.
7. A PDF az adott összehasonlító tervlekérés ellenőrzési pillanatképét tartalmazza. Egy későbbi külön frissítés nem írja át egy korábbi mérés bizonyítékát.

| Átíró | Elfogadott beépített oszloptípus |
|---|---|
| Dátumcast és évkinyerés | pg_catalog.date, pg_catalog.timestamp |
| Aritmetikai átírás | pg_catalog.int2, int4, int8 |
| LIKE egyszerűsítés | pg_catalog.text, varchar |

A státusz **teljesül**, **nem teljesül**, vagy **nem ellenőrizhető**. Domain, saját típus, hiányzó oszlop/jogosultság, kapcsolathiba és a többi átíró esetén ismeretlen marad; ez nem pozitív minősítés. Ismeretlen eredmény mellett a korábbi feltételes futtatás megmarad. A túlcsordulást, kollációt, operátorfeloldást és eredményazonosságot ez a kör nem igazolja. NULL adatot megjelenítünk, de ezeknek az átírásoknak nem általános előfeltétele a NOT NULL.

## Felépítés

- Core: `DatabaseColumnMetadata`, `DatabaseColumnReference`, `RewriteMetadataRequirement`, `RewriteMetadataCheck`, `IDatabaseMetadataService`, `RewriteMetadataEvaluator`.
- Infrastructure: `RewriteColumnResolver` az egyetlen módosított feltétel oszlopát kapcsolja a közvetlen FROM-táblához. Alias, séma és idézőjeles nevek pontosan egyeznek. Nem egyértelmű vagy nem támogatott hivatkozásnál nincs feltételezett feloldás.
- `PostgreSqlMetadataService`: paraméterezett katalóguslekérdezés; azonos `pg_catalog, kiválasztott_séma, pg_temp` search_path, mint az EXPLAIN-nél; 10 másodperces statement timeout és 3 másodperces lock timeout, read-only tranzakció, cancellation token.
- `SqlRewriteComparisonService`: kapcsolat/SQL-eredet ellenőrzése a metaadat-kérés előtt, friss típusvizsgálat a tervlekérés előtt, ismerten hibás típusnál nincs EXPLAIN.
- `RewriteComparisonViewModel` és `SqlRewriteWindow`: külön frissítés, állapotjelzés, tiltás, megszakítás. Javaslatváltás törli a korábbi ellenőrzést.
- `PdfService.Comparison`: a méréshez tartozó metaadat-visszajelzés exportja.

Az adatokat `pg_class`, `pg_namespace`, `pg_attribute`, `pg_type` alapján olvassuk. Egyetlen oszlopot kérünk, nem a teljes sémát. A tábla neve a `to_regclass` paramétere, az azonosítókat külön idézzük; nincs felhasználói név beillesztve SQL-parancsszövegbe. Sémához USAGE, oszlophoz SELECT jogosultság szükséges. Domainnél a nullázhatóságot konzervatívan ismeretlennek jelöljük. A metaadat-kérés külön kapcsolaton történik; egy közben végrehajtott DDL-változtatást nem zár ki.

Kapcsolat nélküli SQL-fájl elemzése változatlan. Fájlból származó EXPLAIN esetén a meglévő eredetellenőrzés miatt előbb az alkalmazásból kell lekérni az eredeti tervet. Jelszót nem tárolunk a visszajelzésben/PDF-ben. Indexek, kulcsok és eredményhalmaz-azonosság vizsgálata későbbi kör.

## Kézi próba szakdoga_lab sémán

Támogatott: `SELECT customer_id FROM szakdoga_lab.customers WHERE EXTRACT(YEAR FROM birth_date) = 1990;`

Tiltandó típus: `SELECT order_id FROM szakdoga_lab.orders WHERE EXTRACT(YEAR FROM order_date) = 2024;`

Az előbbi DATE, az utóbbi TIMESTAMPTZ a megadott tesztsémában. Kérj eredeti tervet, elemezz, majd használd a típusellenőrző gombot. A második javaslatnál a futtatás nem indulhat el.

Automatizált tesztek: típusmátrix, domain/saját/hiányzó típus, alias és idézőjeles feloldás, külön feltételek külön oszlopai, friss ellenőrzés minden futtatáskor, két EXPLAIN-mód tiltása, timeout és megszakítás, PDF-export. A WPF tesztek Windows alatt futtathatók; a fejlesztői Linux-környezetben csak lefordítottuk őket. Élő PostgreSQL-kapcsolatos próba a felhasználó adatbázisán szükséges.
