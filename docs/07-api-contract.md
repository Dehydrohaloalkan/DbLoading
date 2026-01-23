# 7. API контракт (REST + SignalR)

Ниже — рекомендуемый контракт. Его цель: дать фронту стабильные DTO/события и покрыть статусы/запуски/отмену.

## 7.1. REST

### Auth

#### `POST /api/auth/login`

Request:

```json
{
  "dbUsername": "USER1",
  "dbPassword": "******",
  "databaseId": "db-test",
  "managerId": "m1",
  "streamId": "s1"
}
```

Response:

```json
{
  "accessToken": "jwt...",
  "user": {
    "login": "USER1",
    "databaseId": "db-test",
    "managerId": "m1",
    "streamId": "s1"
  }
}
```

Side-effects:

- выставляет HttpOnly cookie с refresh token (см. `08-security-auth.md`)

#### `POST /api/auth/refresh`

- читает refresh token из HttpOnly cookie
- возвращает новый `accessToken`

#### `POST /api/auth/logout`

- отзывает refresh token (и/или “версии сессии” пользователя)
- очищает refresh cookie

### Catalog/config

#### `GET /api/catalog/databases`

Возвращает список DB для dropdown.

#### `GET /api/catalog/streams`

Возвращает manager/streams для dropdown.

#### `GET /api/catalog/scripts`

Возвращает группы/скрипты и доступные колонки (или ссылки на profiles).

Response (пример):

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
          "columnsProfileId": "orders_cols"
        }
      ]
    }
  ],
  "columnsProfiles": [
    {
      "id": "orders_cols",
      "items": [
        { "id": "order_id", "label": "OrderId" },
        { "id": "customer", "label": "Customer" }
      ]
    }
  ]
}
```

### Runs

#### `POST /api/runs`

Запуск выгрузки.

Request:

```json
{
  "mode": "AllGroups",
  "selection": {
    "groups": [
      {
        "groupId": "g-sales",
        "enabled": true,
        "scripts": [
          {
            "scriptId": "s-orders",
            "enabled": true,
            "exportMode": "CustomColumns",
            "selectedColumnItemIds": ["order_id", "customer"]
          }
        ]
      }
    ]
  }
}
```

Response:

```json
{
  "runId": "01HZZ....",
  "status": "Queued"
}
```

#### `GET /api/runs/{runId}`

Текущее состояние run (для восстановления после перезагрузки страницы).

#### `GET /api/runs/{runId}/files`

Опционально: список созданных файлов (для будущего UI).

#### `POST /api/runs/{runId}/cancel`

Отмена run.

## 7.2. Модель статусов (DTO)

Минимальная модель для UI:

- `RunStatus`: `Queued | Running | Success | NoData | Failed | Cancelled`
- `GroupStatus`: агрегированный статус по группе (по худшему/важнейшему состоянию)
- `ScriptStatus`: агрегированный статус по логическому скрипту (учитывая 3 варианта)

Рекомендуемая агрегация:

- если любой вариант `Failed` → скрипт `Failed`
- иначе если все варианты `NoData` → скрипт `NoData`
- иначе если все варианты `Success` → `Success`
- иначе если есть `Running` → `Running`
- иначе `Queued/Cancelled` по аналогии

## 7.3. SignalR

### Hub

- `GET /hubs/runs` (path конфигурируем)

### События (server → client)

#### `run.updated`

Payload:

```json
{
  "runId": "01HZZ....",
  "status": "Running",
  "updatedAt": "2026-01-23T12:34:56.000Z"
}
```

#### `group.updated`

```json
{
  "runId": "01HZZ....",
  "groupId": "g-sales",
  "status": "Running"
}
```

#### `script.updated`

```json
{
  "runId": "01HZZ....",
  "groupId": "g-sales",
  "scriptId": "s-orders",
  "status": "Running",
  "message": "Executing variant 2/3"
}
```

#### `script.progress`

Опционально (если получится измерять):

```json
{
  "runId": "01HZZ....",
  "scriptId": "s-orders",
  "rowsExported": 123456,
  "bytesWritten": 9876543
}
```

### Каналы подписки

Рекомендуется, чтобы клиент передавал `runId` и сервер добавлял connection в группу SignalR:

- `runs:{runId}`

