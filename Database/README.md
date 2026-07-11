# Database

此資料夾用於管理 GroceryStoreGame 的 Supabase PostgreSQL 資料庫。

## 資料夾用途

- `migrations/`：存放建立或修改資料庫結構的 SQL。
- `seeds/`：存放系統初始固定資料的 SQL。

## 執行順序

1. 依編號順序執行 `migrations/` 中的 SQL。
2. 再依編號順序執行 `seeds/` 中的 SQL。

## 注意事項

- 不直接在 Supabase 中任意修改資料表。
- 資料庫結構變更應先新增 migration SQL。
- SQL 經 Pull Request 審查並合併至 main 後，再執行至 Supabase。