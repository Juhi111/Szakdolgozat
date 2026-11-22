-- A szakdoga_lab sémához. Egyenként kérj tervet a kijelölt utasításhoz.
-- 1. Összeadás áthelyezése; egész típus, túlcsordulás nélkül.
SELECT order_id FROM szakdoga_lab.orders WHERE customer_id + 1 > 100;

-- 2. COUNT(*) -> EXISTS (a külső korreláció megmarad).
SELECT c.customer_id FROM szakdoga_lab.customers c
WHERE (SELECT count(*) FROM szakdoga_lab.orders o WHERE o.customer_id = c.customer_id) > 0;

-- 3. COUNT(*) -> NOT EXISTS.
SELECT c.customer_id FROM szakdoga_lab.customers c
WHERE (SELECT count(*) FROM szakdoga_lab.orders o WHERE o.customer_id = c.customer_id) = 0;

-- 4. Wildcard nélküli LIKE -> =.
SELECT customer_id FROM szakdoga_lab.customers WHERE city LIKE 'Budapest';

-- 5. Dátumcast -> napi intervallum. A birth_date DATE típusú.
SELECT customer_id FROM szakdoga_lab.customers WHERE birth_date::date = DATE '1990-02-15';

-- 6. Évkinyerés -> éves intervallum.
SELECT customer_id FROM szakdoga_lab.customers WHERE EXTRACT(YEAR FROM birth_date) = 1990;

-- 7. Csoportosító oszlop szűrése HAVING helyett WHERE-ben.
SELECT customer_id, count(*) FROM szakdoga_lab.orders
GROUP BY customer_id HAVING customer_id > 100 AND count(*) > 2;

-- 8. OR -> IN (olvashatóság, nem garantált gyorsulás).
SELECT customer_id FROM szakdoga_lab.customers WHERE customer_id = 1 OR customer_id = 2 OR customer_id = 3;

-- 9. Egyelemű IN.
SELECT customer_id FROM szakdoga_lab.customers WHERE customer_id IN (100);

-- 10. Ismétlődő IN értékek.
SELECT customer_id FROM szakdoga_lab.customers WHERE customer_id IN (1, 2, 1, 3, 2);
