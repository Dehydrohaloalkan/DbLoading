# 6. Конфигурация

Рекомендуемая структура:

- `config/app.json` — общие настройки сервера (пути, лимиты, параллелизм)
- `config/databases.json` — список доступных DB (dropdown)
- `config/streams.json` — manager/stream (dropdown)
- `config/scripts.json` — логические группы/скрипты и привязка к SQL файлам (3 варианта)
- `config/columns.json` — доступные выражения/колонки для Custom режима

Формат — JSON (нативно для .NET).

## 6.1. config/app.json

```json
{
  "output": {
    "rootPath": "D:/DbLoading/output",
    "scriptsRoot": "scripts",
    "encoding": "utf-8",
    "maxFileBytes": 10485760,
    "cleanupPolicy": "BeforeRunIfPreviousSucceeded",
    "allowOversizeSingleLine": false
  },
  "execution": {
    "laneCount": 3
  },
  "realtime": {
    "signalrEnabled": true
  },
  "auth": {
    "accessTokenMinutes": 20,
    "refreshTokenHours": 4,
    "refreshCookieName": "dbloading_rt"
  }
}
```

Примечания:

- `scriptsRoot` — папка с SQL-файлами; путь относительно родителя `config/` или абсолютный.
- `maxFileBytes` по умолчанию = `10 * 1024 * 1024`.
- `cleanupPolicy` варианты:
  - `BeforeRunAlways`
  - `BeforeRunIfPreviousSucceeded` (по умолчанию)
  - `Never`

## 6.2. config/databases.json

Требование: dropdown “БД” с полями `Server`, `Database`.

```json
[
  { "id": "db-prod", "displayName": "Prod", "server": "db2-prod.company.local", "database": "DBPROD" },
  { "id": "db-test", "displayName": "Test", "server": "db2-test.company.local", "database": "DBTEST" }
]
```

## 6.3. config/streams.json

Требование: manager/stream берутся из конфига на сервере.

```json
{
  "managers": [
    { "id": "m1", "displayName": "Manager 1" }
  ],
  "streams": [
    { "id": "s1", "displayName": "Stream 1" }
  ]
}
```

## 6.4. config/scripts.json

Требования:

- скрипты физически лежат “плоско” в `scripts/`
- группы/скрипты — логическое разделение в конфиге
- один логический скрипт состоит из 3 SQL файлов (варианты)
- варианты пользователю не показываем
- каждый скрипт привязан к `executionLane`

```json
{
  "groups": [
    {
      "id": "g-sales",
      "displayName": "Sales",
      "scripts": [
        {
          "id": "s-orders",
          "displayName": "Orders export",
          "executionLane": 0,
          "variants": [
            { "id": "v1", "sqlFile": "orders_v1.sql" },
            { "id": "v2", "sqlFile": "orders_v2.sql" },
            { "id": "v3", "sqlFile": "orders_v3.sql" }
          ],
          "columnsProfileId": "orders_cols"
        }
      ]
    }
  ]
}
```

Примечания:

- `sqlFile` — имя файла в папке `scripts/` (или относительный путь).
- `columnsProfileId` связывает скрипт с набором доступных выражений в `columns.json`.

## 6.5. config/columns.json

Требования:

- могут быть выражения и алиасы
- пользователь выбирает, какие выражения включить

```json
{
  "profiles": [
    {
      "id": "orders_cols",
      "items": [
        { "id": "order_id", "label": "OrderId", "expression": "t.ORDER_ID" },
        { "id": "customer", "label": "Customer", "expression": "RTRIM(t.CUSTOMER_NAME)" },
        { "id": "amount", "label": "Amount", "expression": "DECIMAL(t.AMOUNT, 18, 2)" }
      ]
    }
  ],
  "serialization": {
    "delimiter": "|",
    "escape": {
      "backslash": "\\\\",
      "pipe": "\\|",
      "cr": "\\\\r",
      "lf": "\\\\n"
    }
  }
}
```

## 6.6. Папка scripts/

- Все `.sql` лежат в `scripts/` “плоско”.
- Именование рекомендуется стандартизировать:
  - `<logicalScriptId>_v1.sql`, `<logicalScriptId>_v2.sql`, `<logicalScriptId>_v3.sql`
- SQL должен начинаться с:
  - `SELECT "LineFile" FROM ...` (допускаются переносы/пробелы)

