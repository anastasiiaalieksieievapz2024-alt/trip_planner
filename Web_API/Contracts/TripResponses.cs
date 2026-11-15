namespace Web_API.Contracts;

/// <summary>Підтвердження бронювання для пункту маршруту.</summary>
public sealed record BookingConfirmation(
    int ItemId,
    string BookingNumber,
    string Provider,
    string Status,
    decimal Price,
    string? ConfirmationDetails);



