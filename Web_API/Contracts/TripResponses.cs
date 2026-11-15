namespace Web_API.Contracts;

/// <summary>Короткі дані поїздки у списку.</summary>
public sealed record TripListItem(
    int TripId,
    string TripName,
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Description,
    string Category);

/// <summary>Підтвердження бронювання для пункту маршруту.</summary>
public sealed record BookingConfirmation(
    int ItemId,
    string BookingNumber,
    string Provider,
    string Status,
    decimal Price,
    string? ConfirmationDetails);

/// <summary>Пункт маршруту поїздки разом із пов'язаними бронюваннями.</summary>
public sealed record RouteStop(
    int ItemId,
    string Type,
    string Title,
    string? Description,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<BookingConfirmation> Bookings);
