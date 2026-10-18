CREATE TABLE customers (
    id         bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name       text        NOT NULL,
    email      text        NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE products (
    id         bigint        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name       text          NOT NULL,
    price      numeric(10,2) NOT NULL CHECK (price >= 0),
    created_at timestamptz   NOT NULL DEFAULT now()
);

CREATE TABLE orders (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id bigint      NOT NULL REFERENCES customers (id),
    status      text        NOT NULL DEFAULT 'pending'
                            CHECK (status IN ('pending', 'shipped', 'cancelled')),
    ordered_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_orders_customer_id       ON orders (customer_id);
CREATE INDEX ix_orders_status_ordered_at ON orders (status, ordered_at);

CREATE TABLE order_items (
    id         bigint        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id   bigint        NOT NULL REFERENCES orders (id) ON DELETE CASCADE,
    product_id bigint        NOT NULL REFERENCES products (id),
    quantity   integer       NOT NULL CHECK (quantity > 0),
    unit_price numeric(10,2) NOT NULL CHECK (unit_price >= 0)
);

CREATE INDEX ix_order_items_order_id ON order_items (order_id);
