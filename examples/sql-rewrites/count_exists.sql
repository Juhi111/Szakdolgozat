SELECT c.customer_id
FROM szakdoga_lab.customers c
WHERE (
    SELECT COUNT(*)
    FROM szakdoga_lab.orders o
    WHERE o.customer_id = c.customer_id
) > 0;