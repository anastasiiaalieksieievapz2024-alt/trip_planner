# Повний k6 тест API

`trip-planner-load.js` проходить усі маршрути, оголошені в API контролерах:

- `GET /api/trips`, `GET /api/trips/{tripId}`, `GET /api/trips/{tripId}/itinerary`
- `POST /api/trips`, `PUT /api/trips/{tripId}`, `DELETE /api/trips/{tripId}`
- `GET /api/trips/{tripId}/members`, `POST /api/trips/{tripId}/members`, `DELETE /api/trips/{tripId}/members/{userId}`
- `GET /api/trips/{tripId}/items`, `GET /api/trips/{tripId}/items/{itemId}`
- `POST /api/trips/{tripId}/items`, `PUT /api/trips/{tripId}/items/{itemId}`, `DELETE /api/trips/{tripId}/items/{itemId}`
- `GET /api/test/public`, `GET /api/test/protected`, `GET /api/test/planner`

Кожна ітерація читає seeded `TripId 1`, а для записів створює окрему тимчасову поїздку, пункт маршруту й учасника. Після перевірки викликаються DELETE для створених учасника, пункту та поїздки. Запускайте сценарій проти тестової бази даних.

## Налаштування

Потрібен свіжий `AUTH_TOKEN` із дозволами:

- `read:trips`, `create:trips`, `update:trips`, `delete:trips`
- `read:itinerary`, `create:itinerary`, `update:itinerary`, `delete:itinerary`
- `read:members`, `create:members`, `delete:members`

`TEST_MEMBER_EMAIL` має бути email іншого, вже зареєстрованого користувача, якого власник токена може додавати й видаляти з поїздки. У БД мають існувати `TripId 1`, категорія поїздки та тип пункту маршруту. Для категорії й типу за замовчуванням використовується ID `1`; за потреби задайте `TRIP_CATEGORY_ID` і `ITINERARY_ITEM_TYPE_ID`.

У PowerShell:

```powershell
$env:BASE_URL = 'https://localhost:7198'
$env:AUTH_TOKEN = '<свіжий access token>'
$env:TEST_MEMBER_EMAIL = '<email іншого зареєстрованого тестового користувача>'
$env:TRIP_CATEGORY_ID = '1'
$env:ITINERARY_ITEM_TYPE_ID = '1'
k6 run Web_API/LoadTests/trip-planner-load.js
```

Підсумок k6 зберігається у `k6-summary.json`. Змініть `RESULT_FILE`, щоб вказати інший шлях.
