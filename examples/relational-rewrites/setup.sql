
BEGIN;
CREATE SCHEMA rewrite_lab;
CREATE TABLE rewrite_lab.customers (customer_id bigint PRIMARY KEY, name text);
CREATE TABLE rewrite_lab.orders (order_id bigint PRIMARY KEY, customer_id bigint NOT NULL);
INSERT INTO rewrite_lab.customers VALUES (1, 'Anna'), (2, 'Béla'), (3, 'Anna'), (4, NULL);
INSERT INTO rewrite_lab.orders VALUES (10, 1), (11, 1), (12, 3);
COMMIT;
