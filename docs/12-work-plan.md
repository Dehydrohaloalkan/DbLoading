# 12. План работ (итерации)

Ниже — план, который позволяет параллельно двигать UI/пайплайн, даже пока DB2 подключение ещё заглушка.

## Итерация 0 — Bootstrap репозитория

- Создать структуру solution/projects для backend (4 проекта: Server/Application/Domain/Infrastructure).
- Создать Angular 21 проект, подключить PrimeNG 21, базовую тему.
- CI/форматирование (EditorConfig, dotnet format, eslint/prettier по желанию).

## Итерация 1 — Конфиги и каталог (без DB2)

- Ввести папку `config/` и JSON схемы (как в `06-config-format.md`).
- Backend endpoints `GET /api/catalog/*`.
- Frontend:
  - экран логина (пока без реального логина)
  - экран main с вкладками групп/списком скриптов и правой панелью выбора колонок.

## Итерация 2 — Auth (JWT + refresh cookie) + revocation

- `POST /api/auth/login` (пока с заглушкой проверки)
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- Angular interceptor + refresh flow.

## Итерация 3 — Run orchestration + статусы

- `POST /api/runs`, `GET /api/runs/{id}`, `POST /api/runs/{id}/cancel`
- Модель статусов `Queued/Running/...`.
- SignalR hub `hubs/runs`:
  - `run.updated`, `group.updated`, `script.updated`

## Итерация 4 — Файловая выгрузка и нарезка 10 MiB

- Реализация writer/slicer по правилам (строка не рвётся, файл не >10 MiB).
- Структура папок `OutputRoot/<runId>/<group>/<script>/<variant>/part-XXXX.txt`.
- Cleanup policy “before run if previous succeeded”.

## Итерация 5 — SQL modification (SELECT "LineFile" → custom)

- Реализовать парсер/замену SELECT‑списка (по описанным допущениям).
- Реализовать сборку выражения склейки через `||` и `|`.
- Строгая валидация: если SQL не модифицируем — error.

## Итерация 6 — DB2 интеграция (твой код)

- Внедрить реальный `IDb2SessionFactory/IDb2Session`.
- Реальный streaming чтения результата (по строкам).
- Тест на одном “эталонном” скрипте.

## Итерация 7 — Stream (заглушка → реальная доставка)

- Сначала заглушка sink (логирование).
- Затем реальная запись:
  - idempotency keys
  - retries/backoff
  - удаление файлов после ack

## Итерация 8 — Полировка UX и эксплуатация

- Детали: фильтры, поиск по скриптам, сводка ошибок, просмотр логов.
- Инсталляция/деплой скрипты.

