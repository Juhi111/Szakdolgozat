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
