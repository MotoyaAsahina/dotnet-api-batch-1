# DotnetApiBatch

共通のデータモデル（Data）を Web API と バッチ（コンソールアプリ）から利用する .NET ソリューションです。
DB は PostgreSQL、データアクセスは EF Core を使います。

## 構成

```
.
├── DotnetApiBatch.slnx
├── compose.yaml                 ローカル開発用の PostgreSQL
├── db/
│   └── init/                    PostgreSQL の初回起動時に実行される SQL
│       ├── 01_schema.sql        スキーマ
│       └── 02_data.sql          サンプルデータ
├── src/
│   ├── DotnetApiBatch.Data/      共通のデータモデル（エンティティ、EF Core の設定、DbContext）
│   ├── DotnetApiBatch.Api/       ASP.NET Core Web API
│   └── DotnetApiBatch.Batch/     バッチ（コンソールアプリ）
└── tests/
    └── DotnetApiBatch.Data.Tests/   Data のテスト（xUnit）。依存関係のルールを確認する
```

### プロジェクト参照

```
DotnetApiBatch.Api ────────┐
DotnetApiBatch.Batch ──────┼──▶ DotnetApiBatch.Data
DotnetApiBatch.Data.Tests ─┘
```

- API と Batch で共有するのは、`Data` のエンティティ・EF Core の設定・DbContext と、用語を定義する絞り込み条件だけです。
- リポジトリとサービス（業務ロジック）は共有せず、`Api` と `Batch` それぞれに置きます。
- `Api` と `Batch` は互いに参照しません。そのため、片方のリポジトリやサービスをもう片方から使うことはできません。
- `Data` は `Api`・`Batch`・ASP.NET Core に依存しません（`Data.Tests` で確認しています）。

## 必要な環境

- .NET SDK 10.0 以上
- Docker（Docker Compose）

## データベース

### 起動

```sh
docker compose up -d
```

PostgreSQL 18 が `localhost:5432` で起動します（DB 名・ユーザー・パスワードはすべて `app`）。
初回起動時に [db/init/](db/init/) の SQL がファイル名順に実行され、スキーマとサンプルデータが投入されます。

### サンプルのスキーマ

| テーブル | 内容 |
|---|---|
| `customers` | 顧客 |
| `products` | 商品 |
| `orders` | 注文。`status` は `pending` / `shipped` / `cancelled` |
| `order_items` | 注文明細。注文時点の単価（`unit_price`）を持つ |

注文日時は投入時点からの相対値なので、いつ投入しても「期限切れの pending 注文」がバッチの対象として存在します。

### データの初期化

`db/init/` の SQL はボリュームが空のときしか実行されません。
SQL を変更したときやデータを最初の状態に戻したいときは、ボリュームごと作り直します。

```sh
docker compose down -v
docker compose up -d
```

### psql で接続

```sh
docker compose exec postgres psql -U app -d app
```

## 設計

### Data（共通）

ファイルは種類（エンティティ・設定）ではなく、ドメインごとのフォルダー（名前空間）に分けます。

```
src/DotnetApiBatch.Data/
├── AppDbContext.cs
├── ServiceCollectionExtensions.cs   AddData()
├── Customers/    Customer / CustomerConfiguration
├── Catalog/      Product / ProductConfiguration
└── Orders/       Order / OrderItem / OrderStatus / 各 Configuration / OrderQueries
```

| 種類 | 内容 |
|---|---|
| エンティティ | 基底クラスは持たず、主キーは各エンティティで定義する |
| `*Configuration` | テーブルとの対応づけ（`IEntityTypeConfiguration<T>`）。`AppDbContext` が `ApplyConfigurationsFromAssembly` でまとめて読み込むので、追加しても DbContext の変更は不要 |
| `*Queries` | API / Batch で共通して使う絞り込み条件（`WithStatus`、`OrderedBefore` など）。「pending」「期限切れ」のような用語の定義だけを置き、処理の流れは置かない |

#### ドメイン間の依存の向き

```
Orders ──▶ Customers
   └─────▶ Catalog
```

- 依存してよいのは Orders → Customers / Catalog の向きだけです。Customers と Catalog は Orders にも互いにも依存しません。
- そのため、`Customer` は注文の一覧（`Customer.Orders`）を持ちません。関連は `OrderConfiguration` で Order 側から定義しています。
- このルールは [ArchitectureTests](tests/DotnetApiBatch.Data.Tests/ArchitectureTests.cs) で確認しています。ドメインを追加したら、許可する向きをテストにも追加します。
- DbContext はドメインをまたいで 1 つです。注文と商品の結合（売上集計など）があるためです。

#### ドメインをプロジェクトに分けるとき

次のどれかに当てはまったら、`DotnetApiBatch.Data.Orders` のようにドメインごとのプロジェクトに分けることを検討します。
フォルダーをドメインごとに分けてあり、依存の向きもテストで固定しているので、フォルダーを移すだけで分けられます。

- チームやリリースの単位がドメインごとに分かれた
- ドメインをまたぐナビゲーションや結合をやめられる（ID だけで参照する）
- ドメインごとに DbContext や DB スキーマを分けたい

#### 共通設定の登録

`AddData()` で、API / Batch 共通の以下の設定を登録します。

- `AppDbContext`（Npgsql、接続リトライ、スネークケースの命名規則）
- `TimeProvider`

接続文字列は各アプリの設定ファイルの `ConnectionStrings:AppDb` から読み込みます。
アプリごとに変えたい Npgsql の設定は引数で渡せます（Batch ではコマンドタイムアウトを延ばしています）。

```csharp
// Api
builder.Services.AddData(builder.Configuration);
// Batch
builder.Services.AddData(builder.Configuration, npgsql => npgsql.CommandTimeout(300));
```

テーブル名・列名は `EFCore.NamingConventions` によりスネークケースに自動で変換されます（`OrderedAt` → `ordered_at`）。
スキーマは `db/init/01_schema.sql` で管理しており、EF Core のマイグレーションは使っていません。

### リポジトリ

リポジトリは API / Batch それぞれのプロジェクトに置き、そのアプリで必要なメソッドだけを持たせます。
両方で使う絞り込み条件は Data に置いて共通化します。

```
Api    Features/Customers/CustomerRepository   ExistsAsync
       Features/Catalog/ProductRepository      ListAsync / ListByIdsAsync
       Features/Orders/OrderRepository         GetDetailAsync / ListByCustomerAsync / Add / SaveChangesAsync
Batch  Repositories/OrderRepository            CancelPendingOrderedBeforeAsync / SumShippedSalesByProductAsync
                 │
                 └── どちらも AppDbContext を直接使い、Data の OrderQueries（WithStatus / OrderedBefore）を組み合わせる
```

- リポジトリは `AppDbContext` をコンストラクターで受け取り、そのアプリの `Program.cs` で `AddScoped` で登録します。
- API と Batch のリポジトリは別のアセンブリにあるので、お互いのメソッドは見えません。
- 両方で使う絞り込み条件は、Data の各ドメインのフォルダーに `IQueryable` の拡張メソッド（`*Queries`）として置きます。

API と Batch では、リポジトリの書き方を次のように分けています。

| | API | Batch |
|---|---|---|
| 参照 | `AsNoTracking` で、レスポンスの形へ直接射影する | 集計クエリ（`GroupBy`）で必要な列だけを取得する |
| 更新 | エンティティを `Add` して `SaveChangesAsync` | `ExecuteUpdateAsync` でまとめて更新し、エンティティを読み込まない |

### API の構成（コントローラー / サービス / リポジトリ）

エンドポイント（コントローラー）は `Controllers/` にまとめ、API の入口を 1 か所で見渡せるようにします。
サービス・リポジトリ・リクエスト / レスポンスの型は、Data と同じドメインごとに `Features/` の下に置きます。

```
src/DotnetApiBatch.Api/
├── Controllers/    エンドポイント。URL のリソースごとに分ける
│   ├── ProductsController     /products
│   ├── CustomersController    /customers/{id}/orders
│   └── OrdersController       /orders
├── Features/       業務ロジックと DB アクセス。ドメインごとに分ける
│   ├── Catalog/    ProductService / ProductRepository / ProductContracts
│   ├── Customers/  CustomerRepository
│   └── Orders/     OrderService / OrderRepository / OrderContracts
└── Errors/         エラーの定義、アプリの例外、エラーレスポンスへの変換
```

| 種類 | 役割 |
|---|---|
| `*Controller` | HTTP の入口。リクエストを受け取り、サービスを呼び、結果を返す |
| `*Service` | 業務ロジック。存在チェックなどの業務上の検証と、リポジトリの組み合わせ |
| `*Repository` | DB アクセス（AppDbContext を使う） |
| `*Contracts` | リクエスト / レスポンスの型 |

```
Controllers/                Features/
ProductsController  ──▶ Catalog/ProductService ──▶ ProductRepository
CustomersController ──┐
OrdersController    ──┴▶ Orders/OrderService   ──▶ CustomerRepository / ProductRepository / OrderRepository
```

- 依存の向きは Controllers → Features → Data です。Features から Controllers は参照しません。
- コントローラーは URL で分け、どのドメインのサービスを呼んでもかまいません（`CustomersController` は `/customers/{id}/orders` のために `OrderService` を呼びます）。
- `Features/` の中でドメインをまたいで使ってよい向きは、Data と同じ Orders → Customers / Catalog です。
- コントローラーには `[ApiController]` を付け、リポジトリを直接は使いません。
- 検証は2段階で行います。

  | 検証の種類 | 例 | どこで |
  |---|---|---|
  | リクエストの形式 | 明細が1件以上、数量が1以上、`status` が正しい値 | `*Contracts` の DataAnnotations。`[ApiController]` が、コントローラーの処理を呼ぶ前に自動で 400 を返す |
  | 業務上の検証 | 顧客・商品が存在するか | サービス。`ValidationFailedException` / `NotFoundException` を投げる |

- リクエストの型は、プロパティで定義した record にしています。positional record（`record X(int A)`）にすると、検証エラーの項目名が C# の名前（`Items`）になってしまうためです。

### エラーレスポンス

エラーは、本番・非本番とも次の形式の JSON（`application/json`）で返します。スタックトレースなどの内部情報は含めず、ログにだけ出力します。

```json
{
  "code": "VALIDATION_ERROR",
  "message": "入力内容に誤りがあります。",
  "details": [
    { "field": "items[0].quantity", "message": "数量は1以上を指定してください。" }
  ],
  "traceId": "00-9a4e72d1...-01"
}
```

| 項目 | 内容 |
|---|---|
| `code` | エラーの種類。クライアントはこれで処理を分ける |
| `message` | 利用者向けのメッセージ |
| `details` | 項目ごとのエラー（検証エラーのときだけ）。本文全体のエラー（JSON が壊れているなど）は `field` を省略する |
| `traceId` | ログと突き合わせるための ID |

#### エラーの定義（[Errors/ErrorDefinitions.cs](src/DotnetApiBatch.Api/Errors/ErrorDefinitions.cs)）

| code | ステータス | 主な発生条件 |
|---|---|---|
| `VALIDATION_ERROR` | 400 | 入力の検証エラー、`ValidationFailedException` |
| `BAD_REQUEST` | 400 | リクエストを読み取れない |
| `NOT_FOUND` | 404 | `NotFoundException`、存在しない URL、ルート制約に一致しない（`/customers/abc/orders`） |
| `METHOD_NOT_ALLOWED` | 405 | 許可されていないメソッド |
| `CONFLICT` | 409 | 一意制約の違反、楽観的同時実行制御の競合 |
| `PAYLOAD_TOO_LARGE` | 413 | 本文が大きすぎる |
| `UNSUPPORTED_MEDIA_TYPE` | 415 | Content-Type が JSON でない |
| `INTERNAL_SERVER_ERROR` | 500 | 想定外の例外すべて |
| `SERVICE_UNAVAILABLE` | 503 | DB に接続できない・応答がない（リトライしても回復しなかった） |

#### エラーレスポンスが作られる場所

| 発生元 | 処理する場所 |
|---|---|
| 例外（アプリの例外、DB の例外、想定外の例外） | [AppExceptionHandler](src/DotnetApiBatch.Api/Errors/AppExceptionHandler.cs)。例外の種類とエラーの定義の対応は `Map()` にまとめている |
| モデル検証エラー（`[ApiController]` の自動 400） | `ApiBehaviorOptions.InvalidModelStateResponseFactory` |
| 本文のないエラー（ルートに一致しない 404、405、415 など） | `UseStatusCodePages()` |

設定は [Errors/ErrorHandlingExtensions.cs](src/DotnetApiBatch.Api/Errors/ErrorHandlingExtensions.cs) の `AddErrorHandling()` / `UseErrorHandling()` にまとめています。
モデルバインディングのエラーメッセージ（既定は英語）も、ここで日本語にしています。

#### エラーを追加するとき

- **新しい種類のエラー**: `ErrorDefinitions` に定義を追加し、`AppException` を継承した例外を作ってサービスから投げます。
- **既存の例外（ライブラリの例外など）のレスポンスを変えたい**: `AppExceptionHandler.Map()` に対応を追加します。

## ビルドとテスト

```sh
dotnet build
dotnet test
```

## 実行

先に `docker compose up -d` で DB を起動しておきます。

### Web API

```sh
dotnet run --project src/DotnetApiBatch.Api
```

| メソッド | パス | 内容 |
|---|---|---|
| GET | `/products` | 商品一覧 |
| GET | `/customers/{id}/orders?status=pending` | 顧客の注文一覧（`status` は省略可） |
| GET | `/orders/{id}` | 注文詳細 |
| POST | `/orders` | 注文作成 |

[src/DotnetApiBatch.Api/DotnetApiBatch.Api.http](src/DotnetApiBatch.Api/DotnetApiBatch.Api.http) からリクエストを送れます。

### バッチ

1つ目の引数で実行するジョブを指定します。

```sh
dotnet run --project src/DotnetApiBatch.Batch -- <ジョブ名>
```

| ジョブ名 | 内容 |
|---|---|
| `cancel-expired-orders` | 一定日数（既定 3 日）を過ぎても `pending` のままの注文を `cancelled` にする |
| `sales-report` | 出荷済みの注文から、商品ごとの売上を集計してログに出力する |

2つ目以降の引数で設定を上書きできます。

```sh
dotnet run --project src/DotnetApiBatch.Batch -- cancel-expired-orders --Jobs:CancelExpiredOrders:ExpireAfterDays 7
```

バッチは1回実行して終了するコンソールアプリです。cron や Kubernetes の CronJob などから定期実行することを想定しています。
成功すると終了コード 0、失敗・キャンセルされたとき、またはジョブ名が正しくないときは 1 を返します。

#### 仕組み

Generic Host（`Host.CreateApplicationBuilder`）の上で動かしているので、API と同じように DI・設定ファイル・ログ・`IOptions<T>` が使えます。

```
src/DotnetApiBatch.Batch/
├── Hosting/        ジョブを動かす仕組み（IBatchJob / JobRunner / AddBatchJob）。個々のジョブには依存しない
├── Jobs/           ジョブ（CancelExpiredOrdersJob / SalesReportJob）
└── Repositories/   Batch 専用のリポジトリ
```

- ジョブは `IBatchJob` を実装し、`Program.cs` で `AddBatchJob<ジョブ>(ジョブ名)` で登録します。ジョブ名をキーにした keyed service として登録され、ジョブ名が正しくないときに表示する一覧にも追加されます。
- `JobRunner`（`BackgroundService`）が、指定されたジョブをスコープを作って1回実行し、終わったら `StopApplication()` でアプリを終了させます。結果は `Environment.ExitCode` に設定します。
- Ctrl+C や SIGTERM（Kubernetes で Pod が止められたときなど）を受けると、ジョブに渡した `CancellationToken` がキャンセルされます。EF Core のクエリに渡しておけば、実行中の SQL も中断されます。

ジョブを追加するときは、`Jobs/` に `IBatchJob` を実装したクラスを作り、`Program.cs` に `AddBatchJob<新しいジョブ>(ジョブ名)` を追加します。

### 環境と接続文字列

接続文字列は、両アプリとも `appsettings.Development.json` にだけ書いています。
`dotnet run` では Development 環境で起動するので、そのまま DB に接続できます。

本番など他の環境では、環境変数で接続文字列を渡します。

```sh
ConnectionStrings__AppDb="Host=...;Database=...;Username=...;Password=..."
```

## プロジェクトの追加

```sh
# 例: Api のテストプロジェクトを追加する
dotnet new xunit -o tests/DotnetApiBatch.Api.Tests
dotnet sln add tests/DotnetApiBatch.Api.Tests
dotnet add tests/DotnetApiBatch.Api.Tests reference src/DotnetApiBatch.Api
```

## 構築時に実行したコマンド

このリポジトリは以下のコマンドで構築しました（.NET SDK 10.0.400）。
最初は共通プロジェクトを `Modules` という名前で作り、後から `Data` に名前を変えています。以下は今の名前で書いています。

```sh
# ソリューションと .gitignore
dotnet new sln -n DotnetApiBatch
dotnet new gitignore

# プロジェクト
dotnet new classlib -o src/DotnetApiBatch.Data
dotnet new webapi   -o src/DotnetApiBatch.Api
dotnet new console  -o src/DotnetApiBatch.Batch
dotnet new xunit    -o tests/DotnetApiBatch.Data.Tests

# ソリューションへの登録
dotnet sln add src/*/*.csproj tests/*/*.csproj

# プロジェクト参照
dotnet add src/DotnetApiBatch.Api reference src/DotnetApiBatch.Data
dotnet add src/DotnetApiBatch.Batch reference src/DotnetApiBatch.Data
dotnet add tests/DotnetApiBatch.Data.Tests reference src/DotnetApiBatch.Data

# パッケージ（EF Core / PostgreSQL）
dotnet add src/DotnetApiBatch.Data package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/DotnetApiBatch.Data package EFCore.NamingConventions
dotnet add src/DotnetApiBatch.Data package Microsoft.Extensions.Configuration.Abstractions

# パッケージ（Batch で DI と設定ファイルを使うため）
dotnet add src/DotnetApiBatch.Batch package Microsoft.Extensions.Hosting

# パッケージ（依存関係のルールをテストするため）
dotnet add tests/DotnetApiBatch.Data.Tests package NetArchTest.Rules
```
