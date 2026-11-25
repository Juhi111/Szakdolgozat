SELECT c.customer_id, c.email
FROM szakdoga_lab.customers AS c
WHERE lower(c.email) = 'teszt@example.com';