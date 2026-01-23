# 8. Безопасность и аутентификация (JWT + refresh cookie)

## 8.1. Цели

- Access token живёт **20 минут**
- Refresh token живёт **4 часа**
- Refresh хранится в **HttpOnly cookie**
- DB2 пароль не попадает в JWT/логи, хранится только в памяти backend
- Возможность **отзывать** токены для конкретного пользователя (logout/revoke)

## 8.2. Поток логина

1. Frontend отправляет `POST /api/auth/login` с DB2 uid/pwd + выбор DB + manager/stream.
2. Backend:
   - валидирует входные данные
   - пытается подключиться к DB2 (или заглушка)
   - при успехе создаёт **in-memory session** для пользователя
3. Backend возвращает:
   - `accessToken` в body
   - `refreshToken` в HttpOnly cookie

## 8.3. Где хранить токены

### Refresh token

- Только в HttpOnly cookie:
  - `HttpOnly=true`
  - `Secure=true` (в HTTPS)
  - `SameSite=Lax` (или `Strict`, если UI/Server на одном домене и нет SSO)

### Access token

Рекомендация для Angular:

- хранить в памяти (service)
- добавлять в `Authorization: Bearer <token>` через interceptor
- не хранить в localStorage, чтобы уменьшить риск XSS‑эксфильтрации

## 8.4. Refresh flow

- При истечении access token клиент вызывает `POST /api/auth/refresh`.
- Backend читает refresh cookie:
  - если refresh валиден и не отозван → выдаёт новый access token
  - опционально: ротация refresh (выдавать новый refresh и инвалидировать старый)

## 8.5. Отзыв токенов (revocation)

Требование: “хорошо бы отзывать токены для определённого пользователя”.

Так как хранилище сессий/refresh токенов — **in-memory**, механизм revocation будет действовать пока работает процесс.

### Вариант A (рекомендован): session version per user

В памяти хранится:

- `userSessionVersion[userId] = int`

При логине/refresh токен содержит claim `sv` (sessionVersion), а при logout/revoke:

- увеличить `userSessionVersion[userId]`

Проверка refresh:

- если `sv` не совпадает с текущим → refresh отклоняется.

Плюсы:

- можно отозвать **все** refresh сессии пользователя одной операцией
- не нужно хранить список токенов

Минусы:

- не отозвать “одно устройство” отдельно (если это понадобится)

### Вариант B: token allowlist/denylist

Refresh токен имеет `jti` (id). В памяти храним:

- `activeRefresh[jti] = userId + expiresAt`

Logout удаляет `jti`.

Плюсы:

- можно отозвать конкретную сессию

Минусы:

- требуется хранить записи на каждый refresh

Можно комбинировать A+B.

## 8.6. Хранение DB2 учётки в памяти

По требованию:

- DB2 uid/pwd хранится только в памяти backend, привязано к “сессии” пользователя.
- Связка “кто это”:
  - userId = login + databaseId + managerId + streamId (или отдельный GUID)
  - активная сессия привязана к refresh/sessionId

Важно:

- при refresh обновление access token не должно требовать повторного DB2 логина
- при logout нужно удалить in-memory DB2 credential/session

## 8.7. CORS/CSRF

Так как refresh в cookie:

- для `POST /api/auth/refresh` и `POST /api/auth/logout` нужен CSRF‑защитный механизм, если UI и API на разных доменах.

Рекомендовано:

- размещать UI и API под одним origin (в проде через reverse proxy)
- или использовать anti‑CSRF token (double submit cookie / header token)

## 8.8. Логи

Запрещено логировать:

- DB password
- refresh token

Разрешено логировать:

- user login (если это не PII по вашим правилам)
- databaseId/managerId/streamId
- runId, groupId, scriptId

