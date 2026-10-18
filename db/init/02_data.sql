-- 空の DB に投入する前提なので、id は 1 から順に採番される

INSERT INTO customers (name, email) VALUES
    ('山田 太郎', 'taro.yamada@example.com'),
    ('佐藤 花子', 'hanako.sato@example.com'),
    ('鈴木 一郎', 'ichiro.suzuki@example.com'),
    ('高橋 美咲', 'misaki.takahashi@example.com'),
    ('田中 健',   'ken.tanaka@example.com');

INSERT INTO products (name, price) VALUES
    ('キーボード',         8800),
    ('マウス',             3200),
    ('モニター 27インチ', 34800),
    ('USB-C ケーブル',     1200),
    ('ノートPCスタンド',   4500),
    ('Webカメラ',          6800);

-- 日時は投入時点からの相対値にして、バッチ（期限切れ注文のキャンセル）の対象が常に存在するようにする
INSERT INTO orders (customer_id, status, ordered_at, updated_at) VALUES
    (1, 'shipped',   now() - interval '20 days', now() - interval '19 days'),
    (1, 'pending',   now() - interval '1 day',   now() - interval '1 day'),
    (2, 'shipped',   now() - interval '15 days', now() - interval '14 days'),
    (2, 'pending',   now() - interval '5 days',  now() - interval '5 days'),
    (3, 'cancelled', now() - interval '30 days', now() - interval '29 days'),
    (3, 'pending',   now() - interval '7 days',  now() - interval '7 days'),
    (4, 'shipped',   now() - interval '10 days', now() - interval '9 days'),
    (5, 'pending',   now() - interval '2 hours', now() - interval '2 hours');

INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES
    (1, 1, 1,  8800),
    (1, 2, 1,  3200),
    (2, 4, 3,  1200),
    (3, 3, 2, 34800),
    (4, 5, 1,  4500),
    (4, 4, 2,  1200),
    (5, 6, 1,  6800),
    (6, 2, 2,  3200),
    (7, 1, 1,  8800),
    (7, 3, 1, 34800),
    (7, 6, 1,  6800),
    (8, 5, 1,  4500);
