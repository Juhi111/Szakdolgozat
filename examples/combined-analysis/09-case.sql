SELECT c.customer_id FROM customers c WHERE CASE WHEN $1 = 'email' THEN c.email ELSE c.phone END = $2;
