SELECT o.order_id
FROM szakdoga_lab.orders o
WHERE EXTRACT(YEAR FROM o.order_date) = 2024;