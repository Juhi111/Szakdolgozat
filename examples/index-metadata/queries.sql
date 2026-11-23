-- Külön-külön futtasd az alkalmazásban, a szakdoga_lab kapcsolattal.
-- A már meglévő lower(email) indexet keressük; nem hozunk létre új indexet.
SELECT c.customer_id, c.email
FROM szakdoga_lab.customers AS c
WHERE lower(c.email) = 'teszt@example.com';

-- Az order_date DESC vezető kulcs lehet jelölt; az INCLUDE oszlopok nem rendezési kulcsok.
SELECT o.order_id, o.customer_id, o.total_amount
FROM szakdoga_lab.orders AS o
ORDER BY o.order_date DESC
LIMIT 100;
