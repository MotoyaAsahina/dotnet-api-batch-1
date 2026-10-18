# エンティティ関連図

`db/init/01_schema.sql` で定義されているテーブルの依存関係を示す。

```mermaid
erDiagram
    customers ||--o{ orders : "places"
    orders ||--|{ order_items : "contains"
    products ||--o{ order_items : "referenced by"

    customers {
        bigint id PK
        text name
        text email
        timestamptz created_at
    }

    products {
        bigint id PK
        text name
        numeric price
        timestamptz created_at
    }

    orders {
        bigint id PK
        bigint customer_id FK
        text status
        timestamptz ordered_at
        timestamptz updated_at
    }

    order_items {
        bigint id PK
        bigint order_id FK
        bigint product_id FK
        integer quantity
        numeric unit_price
    }
```

## 依存関係

- `orders.customer_id` → `customers.id`
- `order_items.order_id` → `orders.id` (`ON DELETE CASCADE`)
- `order_items.product_id` → `products.id`
