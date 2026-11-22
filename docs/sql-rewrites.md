# SQL-szabályok és átírások

A `SqlRewriteRuleFactory.CreateDefault()` kilenc átíró szabálycsaládot regisztrál. Az elemzési és futtatási munkafolyamat változatlan: a javaslatok a meglévő külön ablakban jelennek meg; futtatásuk és összehasonlításuk külön felhasználói művelet.

| Szabály | Támogatott alak | Javasolt változat |
|---|---|---|
| ArithmeticEqualityRewriteRule | `x + 1 > 100`, `101 > x + 1`, `x - 2 = 98` | `x > 99`, `x < 100`, `x = 100` |
| CountExistenceRewriteRule | skalár COUNT(*) > 0 / <> 0 / >= 1 | EXISTS |
| CountExistenceRewriteRule | skalár COUNT(*) = 0 / <= 0 / < 1 | NOT EXISTS |
| LikeLiteralRewriteRule | LIKE / NOT LIKE, aktív wildcard nélkül | = / <>; escape feloldásával |
| DateCastRangeRewriteRule | `x::date = DATE '2024-02-29'` | fél-nyílt napi intervallum |
| YearRangeRewriteRule | EXTRACT(YEAR ...) / date_part('year', ...) = egész év | fél-nyílt éves intervallum |
| HavingGroupKeyRewriteRule | közvetlen GROUP BY oszlop egész literálos HAVING-szűrése | WHERE, a többi HAVING megtartásával |
| OrToInRewriteRule | ugyanazon oszlop homogén literálos egyenlőségei OR-ral | IN lista |
| SingletonInRewriteRule | egyelemű IN / NOT IN | = / <> |
| DuplicateInRewriteRule | ismétlődő IN / NOT IN literálok | ismétlések elhagyása |

## Elemzési bővítések

Új SQL-találatok: `OR_EQUALITY_CHAIN`, `SINGLETON_IN`, `DUPLICATE_IN_VALUES`. Ezek olvashatósági egyszerűsítések, nem mért teljesítményhibák. WHERE és JOIN ON részekben is felismerhetők.

A `COUNT_FOR_EXISTENCE` már a fenti létezési/nemlétezési alakokat és a fordított összehasonlításokat is felismeri. A korábbi, több táblát érintő felismerés megmaradt. A felismerés tágabb lehet az átírásnál: találatból nem feltétlenül készül SQL-alternatíva.

## Korlátok és ellenőrzés

- AST-klónt módosítunk, majd deparse és újraparszolás következik. Az eredeti SQL és eredmény nem változik. Egy javaslat egy feltételt módosít; nem láncoljuk automatikusan az átírásokat.
- Egyetlen SELECT, egy közvetlen FROM-tábla; CTE, JOIN, halmazművelet, SELECT INTO és zárolás esetén az átírók nem generálnak változatot. A WHERE-ben önálló/AND alatti feltételeket kezelünk. Az OR-szabály a teljes egyszerű OR-láncot cseréli.
- Az oszloptípusokat, kollációkat, felhasználói függvényeket/operátorokat nem ellenőrizzük adatbázis-metaadatból. A javaslatok feltételesek, nem igazoltan egyenértékű javítások.
- A dátumos átírás előfeltétele DATE vagy TIMESTAMP WITHOUT TIME ZONE oszlop. TIMESTAMPTZ és INTERVAL esetén nincs igazolt átírás. A tesztadatbázis order_date oszlopa TIMESTAMPTZ, ezért a példák birth_date oszlopot használnak.
- Aritmetikánál beépített egész típus és az eredeti kifejezés túlcsordulásmentessége szükséges. Konstans és új határ INT32 tartományú; szorzás/osztás nincs.
- LIKE esetén text/varchar és megfelelő kollációs viselkedés szükséges. ILIKE, aktív wildcard, hibás escape, char(n)/citext viselkedés nem általánosítható.
- COUNT-átírásnál csak egy közvetlen belső tábla és egyszerű COUNT(*), GROUP BY/HAVING/LIMIT nélkül. A belső szűrésben függvény, cast vagy újabb allekérdezés kizárja az átírást. A korai leállás hibaviselkedését is ellenőrizni kell.
- IN/OR esetén csak homogén, nem NULL egész/szöveg literálok; paraméterek, vegyes típusok, sorértékek kimaradnak.
- HAVING esetén csak közvetlen csoportosító oszlop, önálló/AND alatti összehasonlítás; GROUPING SETS/ROLLUP/CUBE nincs. Aggregátum hibájának elfedése is kizárhatja az alkalmazást.
- A gyorsulást és eredményazonosságot a program nem bizonyítja. Az EXPLAIN-összehasonlítás sem eredményhalmaz-ellenőrzés.
- UNION -> UNION ALL és NOT IN -> NOT EXISTS általános átírás szándékosan nincs: duplikátum- és NULL-szemantika változhat.

## Kipróbálás

Az `examples/sql-rewrites/expanded-rules.sql` a szakdoga_lab sémához tíz bemenetet tartalmaz. Válassz egy utasítást, kérd le a tervét, elemezd, majd nyisd meg az SQL-javaslatok ablakát. A szintetikus példákhoz nem adunk kitalált EXPLAIN-t: a tervet a saját adatbázis adja.

Az új `ExpandedSqlRewriteTests` a támogatott alakokat, escape-kezelést, NULL/vegyes típusok kizárását, dátumhatárokat, korrelációt, HAVING zárójelezést, regisztrált szabályokat és a korábbi COUNT-felismerés megtartását ellenőrzi.

Szemantikai háttér: [PostgreSQL subquery expressions](https://www.postgresql.org/docs/18/functions-subquery.html), [pattern matching](https://www.postgresql.org/docs/18/functions-matching.html), [date/time](https://www.postgresql.org/docs/18/functions-datetime.html).
