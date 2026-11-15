namespace Web_API.Contracts;

/// <summary>JSON-помилка автентифікації, авторизації або відсутнього ресурсу.</summary>
/// <param name="Status">HTTP-код відповіді.</param>
/// <param name="Error">Коротка назва помилки.</param>
/// <param name="Message">Пояснення помилки.</param>
/// <param name="Path">Шлях запиту, для якого виникла помилка.</param>
public sealed record ApiErrorResponse(int Status, string Error, string Message, string? Path);

/// <summary>Успішна відповідь перевірки автентифікованого користувача.</summary>
/// <param name="Message">Повідомлення про успішну автентифікацію.</param>
/// <param name="UserId">Ідентифікатор користувача з access token.</param>
public sealed record AuthenticatedUserResponse(string Message, string? UserId);

/// <summary>Успішна відповідь перевірки дозволу на читання поїздок.</summary>
/// <param name="Message">Повідомлення про наявність дозволу.</param>
/// <param name="Subject">Ідентифікатор суб'єкта з access token.</param>
public sealed record PlannerPermissionResponse(string Message, string? Subject);
