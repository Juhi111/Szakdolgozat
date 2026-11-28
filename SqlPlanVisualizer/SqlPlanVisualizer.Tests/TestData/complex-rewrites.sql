SELECT DISTINCT *
FROM szakdoga_lab.customers AS c
WHERE lower(c.email) LIKE '%gmail.com'
  AND upper(c.city) LIKE 'BUDAPEST'
  AND c.customer_id + 10 > 100
  AND CAST(c.registered_at AS date) >= DATE '2023-01-01'
  AND EXTRACT(YEAR FROM c.registered_at) <= 2026
  AND c.first_name || ' ' || c.last_name LIKE '%a%'
  AND CASE
        WHEN c.marketing_opt_in THEN 1
        ELSE 0
      END = 1
LIMIT 100;

SELECT DISTINCT
    c.customer_id,
    c.first_name,
    c.last_name,
    c.email,
    random() AS random_order
FROM szakdoga_lab.customers AS c
JOIN szakdoga_lab.orders AS o
    ON o.customer_id = c.customer_id
JOIN szakdoga_lab.order_items AS oi
    ON oi.order_id = o.order_id
JOIN szakdoga_lab.products AS p
    ON p.product_id = oi.product_id
WHERE lower(c.email) LIKE '%gmail.com'
  AND CAST(o.order_date AS date) >= DATE '2023-01-01'
  AND o.total_amount + 100 > 500
  AND upper(p.product_name) LIKE '%A%'
  AND oi.quantity > 0
ORDER BY random_order
LIMIT 100;

SELECT
    c.customer_id,
    c.first_name,
    c.last_name,
    c.email
FROM szakdoga_lab.customers AS c
WHERE (
    SELECT COUNT(*)
    FROM szakdoga_lab.orders AS o
    WHERE o.customer_id = c.customer_id
      AND CAST(o.order_date AS date) >= DATE '2023-01-01'
      AND o.total_amount + 50 > 200
) > 0
AND (
    SELECT COUNT(*)
    FROM szakdoga_lab.reviews AS r
    WHERE r.customer_id = c.customer_id
      AND r.rating >= 4
) = 0
AND upper(c.city) LIKE 'BUDAPEST'
ORDER BY c.customer_id
LIMIT 100;

SELECT
    o.customer_id,
    o.sales_channel,
    COUNT(*) AS order_count,
    SUM(o.total_amount) AS total_spent,
    AVG(o.shipping_fee) AS average_shipping_fee,
    MAX(o.order_date) AS last_order_date
FROM szakdoga_lab.orders AS o
WHERE EXTRACT(YEAR FROM o.order_date) >= 2023
  AND CAST(o.order_date AS date) <= DATE '2026-12-31'
  AND o.total_amount + 100 > 300
GROUP BY
    o.customer_id,
    o.sales_channel
HAVING o.customer_id > 100
   AND COUNT(*) > 1
ORDER BY
    total_spent DESC,
    o.customer_id,
    o.sales_channel
LIMIT 100
OFFSET 20000;