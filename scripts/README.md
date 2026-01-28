# SQL-скрипты выгрузки

Все скрипты должны начинаться с `SELECT "LineFile"` (см. `docs/06-config-format.md`, `docs/04-backend-design.md`).

Пример:
```sql
SELECT "LineFile" FROM SYSIBM.SYSDUMMY1
```

Для отсутствующих файлов из `config/scripts.json` можно копировать любой существующий (например `orders_v1.sql`).
