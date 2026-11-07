SELECT c.customer_id, count(*) FROM customers c GROUP BY c.customer_id HAVING c.customer_id > 100;
