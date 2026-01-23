# 10. Observer (наблюдатель) — события и расширяемость

Требование: “реализуй паттерн наблюдатель на запуск скрипта, запуск всех скриптов, запуск группы, и на окончание работы (когда уже произошла выгрузка)”.

Цель: дать возможность добавлять обработчики (например, аудит, метрики, интеграции) без переписывания core‑логики.

## 10.1. Принципы

- Observer работает **внутри одного процесса** backend (без микросервисов).
- Обработчиков может быть несколько, они регистрируются через DI.
- События должны быть **асинхронными** (чтобы можно было писать в лог/метрики/БД).
- Ошибки в observer по умолчанию **не должны ломать** основной экспорт (можно сделать режим “strict”, если нужно).

## 10.2. События

### Event: AllGroupsStart

Срабатывает, когда пользователь запускает “Все группы”.

Минимальные данные:

- `runId`
- пользовательский контекст (login, databaseId, managerId, streamId)
- список groupId, которые включены

### Event: GroupStart

Срабатывает при запуске одной группы (в том числе как часть “All groups”).

Данные:

- `runId`
- `groupId`
- список scriptId, которые включены

### Event: ScriptStart

Срабатывает при старте логического скрипта (до выполнения вариантов).

Данные:

- `runId`
- `groupId`
- `scriptId`
- режим экспорта (Default/Custom)
- список выбранных columnItemIds (если Custom)

### Event: ScriptFinished

Срабатывает после завершения логического скрипта:

- все 3 варианта завершены
- файлы нарезаны и записаны на диск
- (в будущем: после передачи в stream, если это включено)

Данные:

- `runId`, `groupId`, `scriptId`
- итоговый статус (`Success/NoData/Failed/Cancelled`)
- список путей созданных файлов (может быть пустым)
- агрегированные метрики: `rowsExported`, `bytesWritten`, `durationMs`

### Event: RunFinished

Срабатывает, когда полностью завершился run.

Данные:

- `runId`
- итоговый статус
- краткий summary по группам/скриптам

## 10.3. Контракт (пример)

Рекомендуемый интерфейс (концептуально):

- `IExportObserver`
  - `Task OnAllGroupsStart(AllGroupsStartEvent e, CancellationToken ct)`
  - `Task OnGroupStart(GroupStartEvent e, CancellationToken ct)`
  - `Task OnScriptStart(ScriptStartEvent e, CancellationToken ct)`
  - `Task OnScriptFinished(ScriptFinishedEvent e, CancellationToken ct)`
  - `Task OnRunFinished(RunFinishedEvent e, CancellationToken ct)`

И “dispatcher”:

- `IExportObserverDispatcher`
  - вызывает всех зарегистрированных `IExportObserver`
  - собирает ошибки и логирует

## 10.4. Порядок вызовов (гарантии)

- `AllGroupsStart` → затем для каждой группы `GroupStart` → затем для каждого скрипта `ScriptStart` → `ScriptFinished`
- `RunFinished` — всегда последним, даже при ошибке/отмене (если процесс не был аварийно остановлен)

