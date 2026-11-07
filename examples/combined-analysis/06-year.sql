SELECT c.customer_id FROM customers c WHERE extract(year FROM c.created_at) = 2026;
