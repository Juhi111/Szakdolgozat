# Összekapcsolási felületi próbák

Minden JSON mesterséges tesztadat, nem mért PostgreSQL-végrehajtás. A fájlokat az alkalmazásban kell megnyitni; nem kell hozzájuk adatbázis. A paraméteres SQL-ek is elemzési bemenetek.

1. Nyisd meg és elemezd az SQL-fájlt.
2. Töltsd be az azonos nevű JSON-fájlt.
3. Az Összegzés fülön párosítsd az első utasítást és az első tervet.
4. Készítsd el az összegzést. Az elvárt **összekapcsolt** találatok alább szerepelnek. Az önálló SQL- és tervtalálatok külön is megjelenhetnek.

Az alapértelmezett konfigurációval számolunk (különösen a LARGE_OFFSET küszöbértékénél). Az elvárt gépi lista: `expected.json`.

| Fájlpár | Elvárt összekapcsolt találatok |
| --- | --- |
| `01-function` | `FUNCTION_FILTER_WITH_HIGH_REMOVAL` |
| `02-leading-wildcard` | `LEADING_WILDCARD_WITH_HIGH_REMOVAL` |
| `03-like-exact` | `LIKE_WITHOUT_WILDCARD_WITH_HIGH_REMOVAL` |
| `04-cast` | `CAST_FILTER_WITH_HIGH_REMOVAL` |
| `05-arithmetic` | `ARITHMETIC_FILTER_WITH_HIGH_REMOVAL` |
| `06-year` | `YEAR_EXTRACTION_WITH_HIGH_REMOVAL` |
| `07-concatenation` | `CONCATENATION_WITH_HIGH_REMOVAL` |
| `08-optional-parameter` | `OPTIONAL_PARAMETER_WITH_HIGH_REMOVAL` |
| `09-case` | `CASE_FILTER_WITH_HIGH_REMOVAL` |
| `10-distinct` | `DISTINCT_WITH_SPILL` |
| `11-union` | `UNION_WITH_SPILL` |
| `12-group-by` | `GROUP_BY_WITH_SPILL` |
| `13-random` | `ORDER_BY_WITH_SORT_SPILL`, `RANDOM_ORDERING_WITH_SPILL` |
| `14-having` | `HAVING_FILTER_WITH_HIGH_REMOVAL` |
| `15-offset` | `ORDERED_LIMIT_WITH_LARGE_SORT`, `LARGE_OFFSET_WITH_LARGE_SORT` |
| `16-ordered-limit-spill` | `ORDERED_LIMIT_WITH_LARGE_SORT`, `ORDER_BY_WITH_SORT_SPILL` |
| `17-negative-literal` | Nincs; a függvénynév csak szövegliterálban szerepel a tervben. |
