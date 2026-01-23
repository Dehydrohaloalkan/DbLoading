# 11. Локальный HTTPS (Angular + .NET)

Требование: описать процесс, как поднять HTTPS соединение для локальной разработки.

Ниже — практичный вариант для Windows (и общий для Linux/macOS).

## 11.1. Backend (.NET): dev-certs

1. Создать и доверить dev сертификат:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

2. В `DbLoading.Server` включить HTTPS (обычно шаблон ASP.NET уже включает). Для локальной разработки используйте стандартный Kestrel https порт.

3. Проверка:

- открыть `https://localhost:<port>/swagger` (если swagger включён)

## 11.2. Frontend (Angular): HTTPS dev-server

Вариант A (простой): проксировать запросы к backend и использовать HTTPS только на backend.

- Angular dev server остаётся на `http://localhost:4200`
- запросы `/api/*` проксируются на `https://localhost:<backendPort>`

Плюсы: проще.
Минусы: mixed content, если фронт будет на https, а backend на http — поэтому backend должен быть https.

Вариант B (рекомендован): HTTPS и на Angular.

### Способ 1: mkcert (удобно)

1. Установить mkcert и сгенерировать локальный CA:
   - Windows: `choco install mkcert` или `scoop install mkcert`
2. Установить локальный CA:

```bash
mkcert -install
```

3. Выпустить сертификат для `localhost`:

```bash
mkcert localhost 127.0.0.1 ::1
```

Получатся файлы `localhost+*.pem` и `localhost+*-key.pem`.

4. Запуск Angular:

- `ng serve --ssl true --ssl-cert <path-to-cert> --ssl-key <path-to-key>`

### Способ 2: использовать dotnet dev-certs (если удобно)

Можно экспортировать cert и ключ, но это обычно менее удобно, чем mkcert.

## 11.3. CORS (если UI и API на разных портах)

Для локальной разработки (Angular:4200/Backend:5xxx) включить CORS на backend для origin фронта.

В проде лучше сделать один origin через reverse proxy (например, nginx), чтобы уменьшить проблемы с cookie/CSRF.

## 11.4. Cookies и Secure

Если refresh token cookie помечена `Secure=true`, то:

- refresh будет работать только по HTTPS
- локально нужно запускать backend по HTTPS

## 11.5. Рекомендуемая схема локально

- Backend: `https://localhost:5001`
- Frontend: `https://localhost:4200`
- Angular proxy:
  - `/api` → `https://localhost:5001`
  - `/hubs` → `https://localhost:5001`

