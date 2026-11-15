namespace Web_API.Contracts;

/// <summary>Дані щойно створеної поїздки.</summary>
public sealed record TripCreatedResponse(
    int TripId,
    string TripName,
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Description,
    int TripCategoryId);
