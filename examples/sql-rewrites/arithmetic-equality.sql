-- A szakdoga_lab tesztadatbázison kipróbálható átírási jelölt.
-- Előfeltétel: az eredeti customer_id + 1 nem okoz túlcsordulást.
SELECT order_id, customer_id, order_date, total_amount
FROM szakdoga_lab.orders
WHERE customer_id + 1 = 101;
