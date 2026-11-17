# ADO.NET та EF Core — лабораторна робота

Консольний проєкт на .NET 10 для роботи з базою даних планувальника подорожей. У ньому реалізовані приклади ADO.NET, Entity Framework Core, транзакцій, LINQ-запитів, міграцій та concurrency control.

## Вимоги

- .NET SDK 10;
- SQL Server або SQL Server LocalDB;
- рядок підключення `ConnectionStrings:KpzDb` у `appsettings.json`, User Secrets або змінній середовища.

Поточна конфігурація використовує `(localdb)\MSSQLLocalDB` і базу `KpzDb`.

## Запуск

Із кореня репозиторію:

```powershell
dotnet run --project .\Ado_NET\Kpz.Lab2.csproj
```

Під час запуску програма застосовує всі доступні міграції. Вона також додає початкові демонстраційні дані, якщо в базі ще немає маркера `demo.user01@example.test`.

Початковий набір на чистій базі: 10 користувачів, 10 поїздок, 30 учасників, 30 пунктів маршруту, 20 бронювань і 10 документів. У довідниках `TripCategories` та `ItineraryItemTypes` — по 5 записів. Сідер розташований у `Data/DevelopmentDataSeeder.cs`, довідники налаштовані в `Data/EntityConfigurations.cs`.

## Демонстраційні сценарії

Виклики сценаріїв містяться в `Program.cs`. Вони закоментовані, щоб можна було запускати кожну демонстрацію окремо. Розкоментуйте потрібний виклик, запустіть проєкт і перегляньте результат у консолі. Не вмикайте кілька великих блоків одночасно: локальні змінні можуть мати однакові назви, а результати сценаріїв змішаються.

| Пункт | Демонстрація | Файли з реалізацією |
|---|---|---|
| 4 | ADO.NET CRUD: створення, читання, оновлення й видалення користувача та поїздки; параметризовані команди і `SqlDataReader` | `Data/SqlCrudRepository.cs` |
| 5 | Складні SQL-запити: `JOIN`, `LEFT JOIN`, фільтрація дат, `GROUP BY`, `COUNT` і `SUM` | `Data/SqlComplexQueries.cs`, `Data/ComplexQueryDemo.cs` |
| 6 | ADO.NET транзакції: успішний `COMMIT` і `ROLLBACK`, якщо обмеження дат відхиляє некоректний запис | `Data/SqlTransactionDemo.cs` |
| 7 | Метадані моделі EF Core: таблиці, ключі, індекси, зовнішні ключі та правила видалення | `Data/EfModelDemo.cs`, `Data/EntityConfigurations.cs` |
| 8 | EF Core CRUD і транзакційне створення графа поїздки з учасниками, пунктами, бронюванням і документом | `Data/EfCrudBusinessDemo.cs` |
| 9 | LINQ-запити: фільтрація, сортування, проєкція DTO, завантаження зв’язків, агрегування та пагінація | `Data/EfLinqQueriesDemo.cs` |
| 10 | Конфлікт конкурентних змін: два контексти оновлюють один запис із `rowversion` | `Data/EfConcurrencyDemo.cs`, `Data/EntityConfigurations.cs` |
| 13 | Переклад LINQ у SQL та перегляд сформованих запитів через `ToQueryString()` | `Data/EfQuerySqlDemo.cs` |

Більшість демонстрацій створює й прибирає тимчасові записи. ADO.NET сценарій у пункті 6 навмисно фіксує один успішний набір записів через `COMMIT`, тому після нього в базі залишаються користувач, поїздка та пункт маршруту.

## Міграції

Міграції та знімок моделі зберігаються в `Migrations/`. Серед них:

- `InitialCreate` — створення основних таблиць;
- `AddReferenceData` — категорії поїздок і типи пунктів маршруту;
- `AddTripPlanningNotes` — додавання нотаток планування;
- `AddApiReadIndexes` — індекси для запитів Web API.

Команди EF CLI запускайте з папки `Ado_NET`, де розташований маніфест локального інструмента:

```powershell
cd .\Ado_NET
dotnet tool restore
dotnet ef migrations list
dotnet ef database update
```

Застосовані міграції можна перевірити в `dbo.__EFMigrationsHistory`.

## Reverse engineering

Згенерований результат reverse engineering збережений в `Scaffolded/`: `Data/KpzScaffoldedContext.cs` і класи в `Models/`. Ці файли можна порівняти з основною моделлю застосунку в `Data/KpzDbContext.cs`, `Data/EntityConfigurations.cs` і `Models/Entities.cs`.

## SQL-запити EF Core

Пункт 13 показує SQL, сформований для LINQ-фільтрації, сортування, проєкції DTO, завантаження графа сутностей і групування бронювань. `ToQueryString()` повертає текст запиту для перегляду, але не виконує його і не є журналом усіх команд, виконаних під час роботи програми.

