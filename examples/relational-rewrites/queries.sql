-- Az alkalmazásba ezt a fájlt töltsd, ne a setup.sql-t.
-- 1. Mindkét összehasonlított oszlop NOT NULL: NOT IN -> NOT EXISTS.
SELECT c.customer_id, c.name
FROM rewrite_lab.customers c
WHERE c.customer_id NOT IN (SELECT o.customer_id FROM rewrite_lab.orders o);

-- 2. Csak bal oldali oszlopokat kérünk, a jobb oldali order_id NOT NULL.
SELECT c.customer_id, c.name
FROM rewrite_lab.customers c
LEFT JOIN rewrite_lab.orders o ON o.customer_id = c.customer_id
WHERE o.order_id IS NULL;

-- 3. A teljes elsődleges kulcs a kimenetben van: DISTINCT elhagyható.
SELECT DISTINCT c.customer_id, c.name FROM rewrite_lab.customers c;

-- 4. Csak a kapcsolódó sor létezése számít: DISTINCT + JOIN -> EXISTS.
SELECT DISTINCT c.customer_id, c.name
FROM rewrite_lab.customers c
JOIN rewrite_lab.orders o ON o.customer_id = c.customer_id;

-- Ellenpélda: a név nem egyedi; a DISTINCT nem törölhető.
SELECT DISTINCT c.name FROM rewrite_lab.customers c;

-- Metaadatok átírás nélkül: a szűrés oszloptípusa és indexjelöltjei megjelennek.
SELECT c.customer_id FROM rewrite_lab.customers c WHERE lower(c.name) = 'anna';
