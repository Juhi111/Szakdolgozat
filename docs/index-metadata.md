# Indexek és kulcsok

Az elemzési összegzés **Indexek és kulcsok…** gombja külön ablakot nyit. A **Lekérés / frissítés** gomb az eredeti tervben azonosított táblák aktuális indexeit és PRIMARY KEY / UNIQUE megszorításait kéri le.

## Használat

1. Válassz adatbázis-kapcsolatot. PostgreSQL 15 vagy újabb szükséges.
2. Kérj becsült vagy mért tervet az SQL-hez, majd indítsd el a meglévő elemzést.
3. Nyisd meg az összegzést, majd az Indexek és kulcsok ablakot.
4. Kattints a Lekérés / frissítés gombra. A Megállapítások fülön a lehetséges összefüggések, az Index- és kulcsleltár fülön az adatbázisból származó definíciók láthatók.

A jelszó kizárólag az aktív sessionből származik. A lekérés nem menti és nem jeleníti meg. Nincs DDL vagy indexlétrehozás. A katalóguslekérdezés paraméterezett, read-only tranzakcióban fut, 10 másodperces statement és 3 másodperces lock időkorláttal. A Megszakítás gomb és az ablak bezárása megszakítja a lekérést.

## Mit jelent az eredmény?

- **Leltár:** index neve, hozzáférési módja, teljes definíciója, kulcs- és INCLUDE oszlopai, részleges index feltétele, érvényességi állapota és NULL-egyediségkezelése. Külön jelennek meg a PK/UNIQUE megszorítások és halaszthatóságuk.
- **A tervben szereplő index:** pontos tábla-, séma- és indexnév egyezés. A Bitmap Heap Scan alatti bitmap ágak is támogatottak. A megnevezés nem bizonyít tényleges végrehajtást, például nulla loops mellett.
- **Lehetséges jelölt:** egyszerű, egy közvetlen táblás SELECT WHERE feltételében a lower/upper kifejezéshez illő vezető indexkulcs, illetve ORDER BY oszlopokhoz illő B-tree kulcsprefix. A teljes visszafelé olvasás, vegyes rendezési irány és NULL-sorrend ellenőrzött. Az INCLUDE oszlop nem rendezési kulcs.

A lehetséges jelölt **nem bizonyítottan alkalmazható index**. Kolláció, operátorosztály, konverziók, szelektivitás és a teljes terv hatása külön vizsgálatot igényel. Részleges indexnél a predikátum teljesülését sem igazoljuk. Érvénytelen, nem kész vagy törlés alatt álló index nem lesz jelölt. Az üres jelöltlista nem automatikus indexlétrehozási javaslat.

## Korlátok

- Csak az eredeti tervhez tartozó szerver/port/adatbázis/séma/felhasználó kapcsolattal kérhető le. Fájlból betöltött terv esetén előbb kérj adatbázisból tervet.
- A terv és a metaadat külön időpontban készült; az adatbázis azóta változhatott. A felület kiírja a metaadat forrását és idejét.
- A leltár a tervben megnevezett relációkra vonatkozik, nem teljes sémafeltérképezés. Partíciós tábláknál csak a megjelenő relációk adatait mutatja; nem következtet minden partíció indexellátottságára. Legfeljebb 64 külön relációt kérdez le, táblánként külön katalógus-pillanatképpel.
- JOIN, CTE, halmazművelet és allekérdezés esetén nincs automatikus szerkezeti illesztés, de a leltár megtekinthető. Összetett rendezési kifejezések és egyenlőségi szűréssel átugorható indexprefixek nem támogatottak.
- A lower/upper illesztés támogatja az index kifejezésében a text/varchar castot, de csak lehetséges egyezésként.
- Idegen kulcsokat ez a kör nem olvas. Az egyedi index és az UNIQUE megszorítás külön fogalom; részleges egyedi indexből nem következtetünk globális egyediségre.
- Sikertelen vagy jogosultság miatt elérhetetlen metaadat-lekérés nem jelenik meg „nincs index” állításként.
- A meglévő SQL-/tervtalálatokat megőrizzük. Az összegzés lenyitható „Adatbázis alapján pontosított indexjavaslatok” része a lekérés után kiegészíti az általános javaslatokat; ugyanaz az értékelés a PDF-ben is szerepel.

## Ellenőrzés

A `examples/index-metadata/queries.sql` lekérdezéseit külön-külön töltsd be a szakdoga_lab sémához. Meglévő indexeid alapján az elsőnél az `idx_customers_lower_email`, a másodiknál az `idx_orders_date_desc` lehet jelölt. A tervező tényleges indexválasztása adatfüggő. Az INCLUDE mezőknek külön kell szerepelniük. PRIMARY KEY megszorítások a leltár alján láthatók.

Automatizált tesztek: kifejezés és idézett név illesztése, típuskonverzió, részleges/érvénytelen index, INCLUDE kizárása, rendezési irány és NULL-sorrend, bitmap szülőkontextus, sémaütközés, kapcsolateltérés, hiányzó metaadat és megszakítás.

## Az összegzéshez tartozó pillanatkép

Sikeres lekérés után az értékelés, a forrás, a lekérési idő és a felhasznált index-/kulcsdefiníciók változatlan másolata a CombinedAnalysisResult része. A metaadatfrissítés új pillanatképet ad; a korábban készített export nem változik. Sikertelen vagy megszakított frissítés megtartja az előző pillanatképet, annak eredeti dátumával. Részleges lekérés esetén az elérhetetlen táblákról szóló közlések is szerepelnek.

Az összegzés és a PDF egyaránt jelzi, ha a metaadatot még nem kértük le. Ha egy felismert jelöltet a terv is megnevez, a javaslat a meglévő index vizsgálatát kéri új index helyett. Ha nincs felismerhető jelölt, az nem bizonyítja egy index hiányát vagy szükségességét.

A PDF összehasonlításban ez a fejezet az **eredeti tervhez** tartozik; nem állítjuk, hogy a javasolt SQL terve ugyanazt az indexet használja. A fájlból elemzés továbbra is működik metaadat nélkül.

Az előzménymodell opcionálisan képes tárolni és PDF-be exportálni a pillanatképet. A régi bejegyzések olvashatók maradnak. A már elmentett automatikus előzményeket nem írjuk át utólag: az összegzés megnyitásakor mentett bejegyzésben még nincs benne a később lekért metaadat. A későbbi pillanatképet az összegzés PDF-exportjával lehet tartósan megőrizni.

Felületi próba: nyisd meg az összegzést lekérés előtt (ellenőrizetlen állapot), majd kérd le az indexeket a külön ablakban. Az összegzés lenyitható része frissüljön. Exportálj PDF-et, és ellenőrizd az egyező forrást, időpontot, megállapításokat és definíciókat. Megszakított frissítés után a korábbi időpontnak kell megmaradnia.
