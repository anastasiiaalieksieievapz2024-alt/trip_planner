# Trip Planner

Навчальний проєкт планувальника подорожей. Репозиторій містить консольну лабораторну роботу з ADO.NET та EF Core і веб-API для роботи з поїздками, пунктами маршруту й учасниками.

## Структура

| Папка | Призначення |
|---|---|
| [`Ado_NET/`](Ado_NET/) | Консольна демонстрація ADO.NET, EF Core, міграцій, запитів і транзакцій. |
| [`Web_API/`](Web_API/) | ASP.NET Core Web API з JWT-автентифікацією Auth0 та CRUD-операціями. |
| [`api-counters.csv`](api-counters.csv), [`k6-summary.json`](k6-summary.json) | Артефакти навантажувального тестування API. |

`Web_API` посилається на `Ado_NET/Kpz.Lab2.csproj`, тому обидва проєкти використовують спільні EF Core моделі й міграції.

## Вимоги

- .NET SDK 10;
- SQL Server або SQL Server LocalDB (у прикладах використовується `(localdb)\MSSQLLocalDB`);
- для автентифікованих запитів до API — налаштований Auth0 tenant, API та клієнтський застосунок.

Перевірити SDK:

```powershell
dotnet --version
```

## База даних

Обидва застосунки читають рядок підключення `ConnectionStrings:KpzDb`. Типове локальне значення налаштоване у файлах `Ado_NET/appsettings.json` і `Web_API/appsettings.json`.

Під час запуску консольний застосунок і Web API викликають `Database.MigrateAsync()`. Міграції створюють базу й таблиці, а консольний застосунок також додає демонстраційні дані через `DevelopmentDataSeeder`.

Схема містить таблиці:

- `Users`, `Trips`, `TripMembers`;
- `ItineraryItems`, `Bookings`, `TripDocuments`;
- довідники `TripCategories` та `ItineraryItemTypes`.

На чистій базі початковий набір містить 10 користувачів, 10 поїздок, 30 учасників, 30 пунктів маршруту, 20 бронювань, 10 документів і по 5 рядків у кожному довіднику. Сідер перевіряє маркер `demo.user01@example.test` і не додає дублікати при наступних запусках.

## Консольна лабораторна: ADO.NET та EF Core

Запуск з кореня репозиторію:

```powershell
dotnet run --project .\Ado_NET\Kpz.Lab2.csproj
```

У `Ado_NET/Program.cs` демонстраційні виклики закоментовані. Розкоментуйте потрібний сценарій і запускайте його окремо. Так вивід не змішується, а повторне оголошення локальних змінних не спричиняє помилок компіляції.

| Пункт | Що демонструє | Реалізація |
|---|---|---|
| 4 | ADO.NET CRUD для користувача та поїздки | `Data/SqlCrudRepository.cs` |
| 5 | SQL `JOIN`, `LEFT JOIN`, фільтрацію та `GROUP BY` | `Data/SqlComplexQueries.cs`, `Data/ComplexQueryDemo.cs` |
| 6 | ADO.NET `COMMIT` і `ROLLBACK` | `Data/SqlTransactionDemo.cs` |
| 7 | Метадані моделі EF Core | `Data/EfModelDemo.cs`, `Data/EntityConfigurations.cs` |
| 8 | EF Core CRUD і транзакційне створення графа поїздки | `Data/EfCrudBusinessDemo.cs` |
| 9 | LINQ-фільтрацію, сортування, групування та пагінацію | `Data/EfLinqQueriesDemo.cs` |
| 10 | Конфлікт конкурентних змін через `rowversion` | `Data/EfConcurrencyDemo.cs` |
| 13 | SQL, згенерований EF Core з LINQ через `ToQueryString()` | `Data/EfQuerySqlDemo.cs` |

Пункти 11–12, потрібні для демонстрації міграцій і reverse engineering, показуються через файли, перелічені нижче, а не окремим викликом у `Program.cs`.

### Міграції та reverse engineering

Міграції зберігаються в `Ado_NET/Migrations/`. Наприклад:

- `20261111153843_InitialCreate` створює основні таблиці;
- `20261111170126_AddReferenceData` додає таблиці довідників та їхні зв’язки;
- `20261111211043_AddTripPlanningNotes` додає нотатки планування;
- `20261115190000_AddApiReadIndexes` створює індекси для читання API.

Історія застосованих міграцій міститься в `dbo.__EFMigrationsHistory`. Для reverse engineering у `Ado_NET/Scaffolded/` залишені згенерований `KpzScaffoldedContext` і класи моделей.

Для роботи з EF CLI перейдіть у `Ado_NET`:

```powershell
cd .\Ado_NET
dotnet tool restore
dotnet ef migrations list
dotnet ef database update
```

### SQL, сформований ORM

Пункт 13 друкує SQL-представлення чотирьох LINQ-запитів: фільтрація та сортування `Trips`; DTO-проєкція з власником, категорією і кількістю пунктів; завантаження пов’язаних сутностей; групування бронювань з `COUNT` і `SUM`.

`ToQueryString()` показує SQL, який EF Core сформував для запиту, але не виконує сам запит. Це демонстрація генерації SQL, а не автоматичний журнал усіх команд під час роботи застосунку.

## Web API

Запуск із кореня репозиторію:

```powershell
dotnet run --project .\Web_API\Web_API.csproj
```

Для старту мають бути налаштовані `Auth0:Domain`, `Auth0:Audience` і `ConnectionStrings:KpzDb`. Задавайте їх у User Secrets або змінними середовища, а не додавайте секрети в Git:

```powershell
$env:Auth0__Domain = 'your-tenant.auth0.com'
$env:Auth0__Audience = 'https://trip-planner-api'
$env:ConnectionStrings__KpzDb = 'Server=(localdb)\MSSQLLocalDB;Database=KpzDb;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run --project .\Web_API\Web_API.csproj
```

API перевіряє JWT access token від Auth0, права доступу до поїздок і членство користувача. Реалізовано читання, створення, оновлення та видалення поїздок і пунктів маршруту, керування учасниками, параметри фільтрації/сортування/пагінації та кешування читань. Детальні інструкції Auth0 і перелік маршрутів наведені в [`Web_API/README.md`](Web_API/README.md); приклади HTTP-запитів — у [`Web_API/Web_API.http`](Web_API/Web_API.http).

У середовищі Development OpenAPI доступний за `/openapi/v1.json`, а інтерактивний інтерфейс Scalar відкривається на `/scalar`.

## Навантажувальні матеріали

Сценарій k6 і команди запуску описані у [`Web_API/LoadTests/README.md`](Web_API/LoadTests/README.md). Результати, що вже є в репозиторії, збережені в `api-counters.csv` і `k6-summary.json`.
