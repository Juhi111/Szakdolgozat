SELECT c.customer_id FROM customers c WHERE ($1 IS NULL OR c.customer_id = $1);
