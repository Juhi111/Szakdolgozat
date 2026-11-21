# Szakdolgozat
SZTE Szakdolgozat


## SQL–EXPLAIN összekapcsolás

- [Működés, támogatott kapcsolatok és korlátok](docs/combined-analysis.md)
- [Felületi tesztek: SQL–JSON mintapárok](examples/combined-analysis/README.md)


## Mentett adatbázis-kapcsolatok

Az **Adatbázis → Kapcsolat beállítása…** ablakban a kapcsolati beállítások becenévvel menthetők.

1. Adj nevet a kapcsolatnak, és töltsd ki a szervert, portot, adatbázist, felhasználónevet, sémát és TLS-beállítást.
2. A **Profil mentése** gomb jelszó és kapcsolatteszt nélkül ment. A kijelölt profil módosítható és átnevezhető; másik profilhoz válaszd az **Új profil** gombot. A nevek kis- és nagybetűtől függetlenül egyediek.
3. Következő indításkor válassz a **Mentett kapcsolatok** listából. A beállítások kitöltődnek, a jelszómező üres. Add meg a jelszót, majd válaszd a **Kapcsolat használata** gombot.
4. A **Kijelölt profil törlése** megerősítés után törli a mentést. Ez nem felejti el a már használatra kiválasztott, memóriában tárolt kapcsolatot.

A profilok helye: `%LOCALAPPDATA%\SqlPlanVisualizer\database-connections.json`.
A fájl verziózott JSON, jelszót nem tartalmaz. A jelszó csak a futó alkalmazás sessionjében marad meg.
A mentés ideiglenes fájlon keresztül történik. Hibás vagy nem támogatott profilfájlt az alkalmazás nem ír felül; javítás után a **Lista újratöltése** gombbal lehet újra próbálkozni.

Kézi ellenőrzés Windows alatt: ments egy profilt, indítsd újra az alkalmazást, töltsd vissza, és ellenőrizd a mezőket és az üres jelszómezőt. Próbálj átnevezést, azonos névvel másik profil mentését, profilváltást beírt jelszó után, majd törlést. A profilváltásnak törölnie kell a jelszómezőt; a Mentés önmagában nem változtatja meg az aktív sessiont.

## Adatbázisos tervlekérés és tesztek

Az **Adatbázis** menüben külön művelet a becsült terv lekérése és a SELECT-et végrehajtó mért terv lekérése. Futás közben mindkét menüpont letiltott, az állapotsor folyamatjelzőt mutat. Az Üzenetek fül rögzíti az indulást és az eredményt; szerverhiba esetén a PostgreSQL hibakódját és üzenetét is. A terv betöltése nem indít automatikus elemzést.

A tesztek két projektben vannak, linkelt Desktop-források nélkül:

- `SqlPlanVisualizer.Tests`: Core/Infrastructure tesztek, Linuxon és Windowson futtathatók.
- `SqlPlanVisualizer.Desktop.Tests`: a tényleges Desktop projektre hivatkozó ViewModel- és parancstesztek, Windows és .NET 8 Desktop runtime szükséges a futtatásukhoz.

Windows alatt a teljes készlet:

```sh
dotnet test SqlPlanVisualizer/SqlPlanVisualizer.sln
```

Linux alatt:

```sh
dotnet test SqlPlanVisualizer/SqlPlanVisualizer.Tests/SqlPlanVisualizer.Tests.csproj
```

Kézi ellenőrzés: válassz kapcsolatot, tölts be egy SELECT-et, és kérj becsült, majd mért tervet. A mért tervben legyen Actual Rows/Actual Loops, a javaslatok csak az Elemzés után jelenjenek meg. Hibás táblanévvel ellenőrizd, hogy a hiba megjelenik az Üzenetek fülön, és a korábbi terv megmarad.

## Terv eredete és automatikus párosítás

Az adatbázisból lekért terv változatlan forráspillanatképet kap: az elküldött SQL, az utasítás eredeti sorszáma, a teljes SQL-dokumentum SHA-256 lenyomata, szerver/port, adatbázis, séma, felhasználó, a lekérés kezdő időpontja, valamint becsült/mért mód. Jelszó és connection string nem kerül ebbe az objektumba. A kapcsolat későbbi átállítása nem módosítja a korábban rögzített eredetet.

A lekérés továbbra is csak betölti a tervet. Az **Elemzés** után az Összegzés fül automatikusan kiválasztja és párosítja a terv eredeti utasítását. Az **Összegzés készítése** gomb nyitja meg a jelentést; adatbázisos tervnél ehhez nem szükséges kézi párosítás. Másik utasításra váltva nem lehet téves párral jelentést készíteni.

Az ellenőrzés szándékosan szigorú: a dokumentum, az utasítás sorszáma és a kivágott SQL-szöveg is egyezzen. Azonos SQL-t tartalmazó két külön utasítás nem cserélhető fel. Bármilyen dokumentummódosítás után új terv szükséges, vagy az eredeti dokumentumot kell visszatölteni és újraelemezni. Újraelemzéskor az újonnan létrejött utasításobjektumokhoz is helyreáll a megfelelő párosítás.

JSON-fájlból betöltött tervhez csak a fájlnév és a betöltési idő ismert; nem következtetünk az eredeti adatbázisra, SQL-re vagy készítési időre. Itt megmarad a kézi párosítás. A korábban mentett, eredetadat nélküli előzmények továbbra is betölthetők.

Az eredetadat megjelenik az összegzésben, a PDF-ben és az előzmények **Terv eredete** fülén. A forrásfájlok mentésének kikapcsolásakor az eredetbe ágyazott SQL-t is elhagyjuk az előzményből; a metaadatok megmaradnak. A PDF továbbra sem exportálja automatikusan a teljes SQL-bemenetet.

Kézi ellenőrzés Windows alatt:
1. Egyetlen SELECT-re kérj mért tervet, majd indíts elemzést: az Összegzés fül automatikus párosítást jelezzen.
2. Két utasításos fájlban válaszd a másodikat, kérj tervet, majd elemezd újra: a második legyen kiválasztva.
3. Válassz másik utasítást: az összegzés létrehozását utasítsa el.
4. Cseréld le az SQL-t és elemezd: a régi tervet ne párosítsa hozzá. Az eredeti dokumentum visszatöltése és elemzése helyreállíthatja a párost.
5. Tölts be JSON-fájlt: az új terv eredete fájlos legyen, kézi párosítással.
6. Készíts PDF-et és előzményt; ellenőrizd az eredetadatokat, valamint kikapcsolt forrásmentés mellett az SQL hiányát az előzmény JSON-jából.

## SQL-javítási javaslatok – első változat

Az összegző ablak **SQL-javítási javaslatok** részében az eredeti utasítás mellett külön, kijelölhető/másolható SQL-változat jelenik meg. A javaslat előállítása nem módosítja a betöltött szöveget és nem futtat SQL-t. Külön gombokkal tervet kérhetsz vagy mérést indíthatsz (lásd alább).

Az első szabály az egy táblát közvetlenül olvasó SELECT felső szintű WHERE feltételében felismeri az `oszlop + egész = egész` és `oszlop - egész = egész` alakot, önállóan vagy AND alatt. Az átírás AST-klónon történik, a parser deparserével állítjuk elő és újraparszoljuk az SQL-t. Egy javaslat egy feltételt változtat meg; több találat esetén külön alternatívák keletkeznek. A megjegyzések kimaradnak, a formázás változhat. A konstansok és az eredmény az int32 tartományban maradnak.

Ezek **feltételes javaslatok**, nem bizonyítottan egyenértékű javítások. Ellenőrizni kell a beépített egész oszloptípust, a szokásos PostgreSQL-operátorokat és azt, hogy az eredeti művelet nem csordulhat túl. Az alkalmazás ebben a fázisban nem ellenőrzi az adatbázis oszloptípusait vagy értéktartományait. Az eredményazonosságot nem ellenőrzi; a külön indított mérések időadatait összehasonlítja. Szorzás/osztás, OR/NOT alatti feltétel, paraméter, cast, JOIN és CTE jelenleg nem támogatott átírási eset.

Kipróbálás:
1. Töltsd be az `examples/sql-rewrites/arithmetic-equality.sql` fájlt.
2. Válaszd ki a tesztadatbázis kapcsolatát és kérd le a tervet, majd nyomd meg az **Elemzés** menüpontot.
3. Nyisd meg az összegző ablakot: a javasolt SQL-ben `customer_id = 100` szerepel.
4. Az eredeti SQL maradjon változatlan. Több utasítás esetén csak az összegzéshez kiválasztott utasításból készül javaslat. Fájlból betöltött, kézzel párosított terv mellett is működik, mert az elemzett SQL-pillanatképből dolgozik.

Új átíráshoz implementáld az `ISqlRewriteRule` interfészt, majd add hozzá az összegző ViewModelben létrehozott `SqlRewriteService` szabálylistájához. A javaslatnak mindig tartalmaznia kell az eredeti és javasolt SQL-t, magyarázatot és alkalmazási feltételeket. A javaslatok és összehasonlításuk PDF-/előzménymentése későbbi lépés; a meglévő elemzési jelentés mentése változatlan.

### Javaslat tervlekérése és összehasonlítása

Az összegző ablakban a **Javaslat tervlekérése és összehasonlítása** részben válassz SQL-változatot, majd kérj becsült tervet vagy indíts mérést. A **Futtatás és mérés** megerősítést kér; a becsült lekérés nem kapcsolja be az ANALYZE módot. A futó lekérés megszakítható, az ablak bezárása is megszakítást kér. Egy ablakban egyszerre egy lekérés indítható.

Az eredeti tervnek az alkalmazásból, az eredeti SQL-hez lekért tervnek kell lennie. A kiválasztott szerver, port, adatbázis, séma és felhasználó egyezését ellenőrizzük. Fájlból betöltött vagy eltérő kapcsolathoz tartozó terv esetén előbb kérj új eredeti tervet. Ez a kapcsolatbeállítások egyezését igazolja, nem az adatbázis változatlanságát vagy fizikai szerverazonosságát.

A javasolt tervet külön tároljuk az ablakban, és a meglévő SQL-/tervelemző szolgáltatásokkal elemezzük. Egymás mellett látható a két tervfa, a tervezési és futási idő, a gyökér költsége és sorszáma, valamint a találatszámok; a javaslat részletes találatai lenyithatók. A főablak SQL-je, terve és eredeti elemzése nem változik. Új lekérés vagy másik javaslat választása lecseréli az ablak előző összehasonlítását.

Futásiidő-arány csak két mért terv pozitív, véges időadataiból készül. Külön időpontban végzett mérésekről van szó: az adatok, indexek, terhelés, cache és beállítások változhattak. Az **eredményazonosság továbbra sincs ellenőrizve**; a sorszámok vagy a találatszámok egyezése nem bizonyítja azt. A két változat ismételt, kontrollált mérése, eredményellenőrzése és az összehasonlítás PDF-/előzménymentése még külön fejlesztés.

Felületi ellenőrzés Windows alatt: eredeti mért terv → Elemzés → Összegzés → javaslat kiválasztása → becsült lekérés (nincs időarány), majd megerősített mérés (időarány, ha mindkét idő pozitív). Ellenőrizd a megszakítást és azt is, hogy másik kapcsolat kiválasztása nem indít lekérést.
