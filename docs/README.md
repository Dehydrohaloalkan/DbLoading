# DbLoading — документация

Эта папка содержит спецификацию, архитектуру, правила и план работ для проекта **DbLoading** (клиент‑серверное приложение для выгрузки данных из DB2 по заранее определённым SQL‑скриптам, с нарезкой файлов по 10 MiB).

## Содержание

- `01-vision-and-scope.md` — цели, ограничения, границы системы
- `02-requirements.md` — функциональные/нефункциональные требования
- `03-architecture-overview.md` — обзор архитектуры и ключевые решения
- `04-backend-design.md` — дизайн backend (.NET 10), доменные модели, потоки, storage, observer
- `05-frontend-design.md` — дизайн frontend (Angular 21 + PrimeNG 21), UX/экраны
- `06-config-format.md` — форматы конфигов (папка `config/`), примеры JSON
- `07-api-contract.md` — REST API + SignalR события/контракты
- `08-security-auth.md` — JWT/cookies, refresh, отзыв токенов, хранение DB‑учётки в памяти
- `09-data-export-pipeline.md` — пайплайн “выполнить → выгрузить → нарезать → (стрим) → cleanup”
- `10-observer-events.md` — события наблюдателя/расширяемость
- `11-local-dev-https.md` — как поднять HTTPS для локальной разработки (Angular + .NET)
- `12-work-plan.md` — план работ/итерации
- `13-coding-standards.md` — общие правила, соглашения, качество
- `14-glossary.md` — термины

