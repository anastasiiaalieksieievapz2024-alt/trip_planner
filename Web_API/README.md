# Web API — Trip Planner

ASP.NET Core Web API на .NET 10 для керування поїздками, пунктами маршруту та учасниками. API використовує спільні з `Ado_NET` EF Core сутності й міграції.

## Вимоги

- .NET SDK 10;
- SQL Server або SQL Server LocalDB;
- Auth0 tenant, зареєстрований API та клієнтський Native/SPA застосунок для отримання access token.

## Налаштування

API читає такі значення конфігурації:

| Ключ | Призначення |
|---|---|
| `ConnectionStrings:KpzDb` | Рядок підключення до бази SQL Server. |
| `Auth0:Domain` | Домен tenant Auth0 без `https://`, наприклад `your-tenant.auth0.com`. |
| `Auth0:Audience` | Identifier API в Auth0; типовий приклад у проєкті — `https://trip-planner-api`. |

Для локального запуску можна задати параметри в PowerShell. Підставте власний Auth0 domain та audience:

```powershell
$env:Auth0__Domain = 'your-tenant.auth0.com'
$env:Auth0__Audience = 'https://trip-planner-api'
$env:ConnectionStrings__KpzDb = 'Server=(localdb)\MSSQLLocalDB;Database=KpzDb;Trusted_Connection=True;TrustServerCertificate=True'
```

Не зберігайте секрети клієнта чи токени в Git. Значення `Auth0__Domain` та `Auth0__Audience` не є клієнтським секретом; для локальних секретних налаштувань використовуйте User Secrets.

### Налаштування Auth0

1. Створіть у Auth0 окремий API з identifier, який збігається з `Auth0:Audience`.
2. Увімкніть RBAC і додавання дозволів до access token.
3. Створіть Native або SPA application і використовуйте Authorization Code Flow з PKCE.
4. Отримуйте **access token саме для цього API** й надсилайте його як `Authorization: Bearer <access_token>`.
5. Додайте потрібні дозволи зі списку нижче до API та клієнта.

Не використовуйте Auth0 Management API audience (`https://<tenant>/api/v2/`) як audience планувальника. API перевіряє підпис токена, issuer, audience і строк дії через JWT Bearer middleware. Авторизація читає дозволи з claim `permissions` або OAuth claim `scope`.

Щоб зіставити користувача токена з рядком у `dbo.Users`, токен має містити email claim `email` або `preferred_username`, а відповідний email має бути зареєстрований у базі. Консольний проєкт `Ado_NET` додає початкові демонстраційні записи; сам Web API виконує міграції, але не запускає цей development seeder.

## Запуск

З кореня репозиторію після налаштування змінних середовища:

```powershell
dotnet run --project .\Web_API\Web_API.csproj
```

Профіль HTTPS за замовчуванням використовує `https://localhost:7198`; HTTP — `http://localhost:5266`. Під час запуску API застосовує наявні міграції до `KpzDb`.

У Development доступні OpenAPI документ `/openapi/v1.json` та інтерфейс Scalar `/scalar`. HTTP-запити для IDE збережені у [`Web_API.http`](Web_API.http).

## Маршрути API

Усі маршрути, крім `GET /api/test/public`, вимагають валідний access token. Захищені кінцеві точки також перевіряють відповідний дозвіл.

| Метод | Шлях | Дозвіл | Призначення |
|---|---|---|---|
| GET | `/api/test/public` | — | Публічна перевірка доступності. |
| GET | `/api/test/protected` | Автентифікація | Перевірка access token; повертає `sub`. |
| GET | `/api/test/planner` | `read:trips` | Перевірка дозволу читання поїздок. |
| GET | `/api/trips` | `read:trips` | Сторінковий список власних і спільних поїздок. |
| GET | `/api/trips/{tripId}` | `read:trips` | Деталі поїздки, доступної користувачу. |
| GET | `/api/trips/{tripId}/itinerary` | `read:itinerary` | Сторінковий маршрут із даними бронювань. |
| POST | `/api/trips` | `create:trips` | Створення поїздки. |
| PUT | `/api/trips/{tripId}` | `update:trips` | Оновлення поїздки її власником. |
| DELETE | `/api/trips/{tripId}` | `delete:trips` | Видалення поїздки її власником. |
| GET | `/api/trips/{tripId}/items` | `read:itinerary` | Сторінковий список пунктів маршруту. |
| GET | `/api/trips/{tripId}/items/{itemId}` | `read:itinerary` | Один пункт маршруту. |
| POST | `/api/trips/{tripId}/items` | `create:itinerary` | Додавання пункту маршруту. |
| PUT | `/api/trips/{tripId}/items/{itemId}` | `update:itinerary` | Оновлення пункту маршруту. |
| DELETE | `/api/trips/{tripId}/items/{itemId}` | `delete:itinerary` | Видалення пункту маршруту. |
| GET | `/api/trips/{tripId}/members` | `read:members` | Сторінковий список учасників. |
| POST | `/api/trips/{tripId}/members` | `create:members` | Додавання зареєстрованого користувача як читача за email. |
| DELETE | `/api/trips/{tripId}/members/{userId}` | `delete:members` | Видалення учасника власником поїздки. |

Права `read:*` на ресурси поїздки доповнюються перевіркою, чи є користувач її власником або учасником. Мутації поїздки й керування учасниками виконуються лише власником згідно з правилами сервісу.

## Фільтрація, сортування й пагінація

Колекційні кінцеві точки приймають `page`, `pageSize`, `sortBy` і `sortDirection`. Сторінки нумеруються з 1; максимальний `pageSize` — 100. Доступні поля сортування перевіряються для кожного типу колекції.

Додаткові фільтри:

- поїздки: `destination`, `category`;
- пункти маршруту: `title`, `type`, `from`, `to`;
- учасники: `search`, `role`.

Наприклад, щоб отримати другу сторінку поїздок за напрямком із сортуванням від найновішої дати:

```http
GET https://localhost:7198/api/trips?page=2&pageSize=5&destination=Kyiv&sortBy=startDate&sortDirection=desc
Authorization: Bearer <access_token>
```

Відповідь містить `items`, номер сторінки, її розмір, загальну кількість записів і сторінок.

## Помилки й поведінка

- `401` — токен відсутній, прострочений або недійсний;
- `403` — токен валідний, але не має потрібного дозволу;
- `404` — ресурс не існує або недоступний поточному користувачу;
- `400` — помилка перевірки параметрів чи тіла запиту;
- `409` — наприклад, користувач уже є учасником поїздки;
- `422` — дати пункту маршруту не вкладаються в дати поїздки.

У проєкті також налаштовані user-scoped output cache, стиснення відповідей, централізована обробка винятків і очищення кешу після змін.

## Навантажувальний сценарій

k6-скрипт, потрібні дозволи, змінні середовища та інструкції збережені у [`LoadTests/README.md`](LoadTests/README.md) і [`LoadTests/trip-planner-load.js`](LoadTests/trip-planner-load.js). Запускайте навантаження проти тестової бази.