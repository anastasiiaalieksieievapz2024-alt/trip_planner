# Web API — планувальник подорожі

ASP.NET Core Web API на .NET 10 для варіанта №9. API підключає моделі та EF Core міграції з `Ado_NET` і повертає дані планера: поїздки користувача, хронологічні пункти маршруту та підтвердження бронювань.

## Auth0: перевірка JWT access token

API працює як resource server: клієнт отримує access token через Authorization Code Flow з PKCE та надсилає його в `Authorization: Bearer <access_token>`. API не виконує інтерактивний sign-in і не приймає ID tokens замість access tokens.

У Auth0 створіть окремий API з identifier `https://trip-planner-api` та увімкніть для клієнтської програми потрібні дозволи. Створіть Native/SPA application і налаштуйте Authorization Code Flow з PKCE та дозволені callback URL. Клієнт запитує access token для API з audience `https://trip-planner-api` і потрібним scope. Audience має точно збігатися з identifier API в Auth0. Не використовуйте `https://dev-p6goqvx1cam1gt2v.us.auth0.com/api/v2/` як audience цього застосунку: це Auth0 Management API.

У `appsettings.json` задано домен Auth0 та audience API. Передавайте секрети/перевизначення налаштувань через змінні середовища у форматі `Auth0__Domain` і `Auth0__Audience`, якщо потрібно змінити їх локально. Домен вказується без `https://`; API формує issuer `https://<domain>/` автоматично.

Bearer middleware завантажує OIDC metadata та ключі підпису Auth0 і перевіряє issuer, підпис, audience і строк дії токена. Усі endpoints захищені політикою за замовчуванням, крім маршрутів із `[AllowAnonymous]`. Запуск застосовує наявні міграції `Ado_NET` до `KpzDb`.

Для запуску задайте рядок підключення до SQL Server за потреби й виконайте:

```powershell
$env:ConnectionStrings__KpzDb = 'Server=(localdb)\MSSQLLocalDB;Database=KpzDb;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run --project .\Web_API.csproj
```

## Ендпоїнти

У Development OpenAPI документ доступний за `/openapi/v1.json`. Приклади запитів — у `Web_API.http`.

| Метод | Шлях | Доступ | Призначення |
|---|---|---|---|
| GET | `/api/test/public` | Без токена | Перевірка доступності API |
| GET | `/api/test/protected` | Дійсний Auth0 access token | Перевірка автентифікації; повертає `sub` користувача |
| GET | `/api/trips` | Дійсний Auth0 access token | Власні та спільні поїздки користувача |
| GET | `/api/trips/{tripId}/itinerary` | Дійсний Auth0 access token | Хронологічний маршрут із підтвердженнями бронювань |

Щоб зіставити Auth0 користувача з `Users.Email`, access token має містити email claim (`email` або `preferred_username`). Якщо claim відсутній або email не відповідає запису в базі, список поїздок буде порожнім.
