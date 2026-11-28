-- Hat átírás egyetlen javaslatban:
-- két aritmetika, egy LIKE, két egyelemű IN (az egyik a duplikációszűrés után), egy ismétlődő IN.
-- A futtatáshoz a szakdoga_lab tesztadatbázis szükséges.
SELECT c.customer_id, c.email, c.city
FROM szakdoga_lab.customers AS c
WHERE c.customer_id + 10 > 100
  AND c.customer_id - 2 < 50000
  AND c.city LIKE 'Budapest'
  AND c.country_code IN ('HU', 'HU')
  AND c.customer_status IN ('active')
ORDER BY c.customer_id
LIMIT 100;

-- Az összes HAVING-feltétel áthelyezhető; az összesítő feltétel megmarad.
SELECT o.customer_id, COUNT(*) AS order_count
FROM szakdoga_lab.orders AS o
WHERE o.customer_id + 1 > 100
GROUP BY o.customer_id
HAVING o.customer_id > 200
   AND o.customer_id < 40000
   AND COUNT(*) > 1
ORDER BY o.customer_id
LIMIT 100;
