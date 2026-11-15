using System.ComponentModel.DataAnnotations;

namespace Web_API.Contracts;

/// <summary>Спільні змінювані поля пункту маршруту.</summary>
public abstract class ItineraryItemWriteDto : IValidatableObject
{
    [Range(1, int.MaxValue)] public int ItineraryItemTypeId { get; init; }
    [Required, StringLength(200)] public string Title { get; init; } = string.Empty;
    [StringLength(2000)] public string? Description { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    [StringLength(400)] public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDateTime == default) yield return new ValidationResult("Вкажіть дату й час початку.", [nameof(StartDateTime)]);
        if (EndDateTime == default) yield return new ValidationResult("Вкажіть дату й час завершення.", [nameof(EndDateTime)]);
        if (EndDateTime < StartDateTime) yield return new ValidationResult("Завершення має бути не раніше початку.", [nameof(EndDateTime)]);
        if (Latitude is < -90 or > 90) yield return new ValidationResult("Широта має бути від -90 до 90.", [nameof(Latitude)]);
        if (Longitude is < -180 or > 180) yield return new ValidationResult("Довгота має бути від -180 до 180.", [nameof(Longitude)]);
    }
}

/// <summary>Дані для створення пункту маршруту.</summary>
public sealed class CreateItineraryItemDto : ItineraryItemWriteDto { }

/// <summary>Повний набір змінюваних полів пункту під час оновлення.</summary>
public sealed class UpdateItineraryItemDto : ItineraryItemWriteDto { }

/// <summary>Пункт маршруту для читання разом із підтвердженнями бронювань.</summary>
public sealed record ItineraryItemReadDto(int ItemId, string Type, string Title, string? Description,
    DateTime StartDateTime, DateTime EndDateTime, string? Address, decimal? Latitude,
    decimal? Longitude, IReadOnlyList<BookingConfirmation> Bookings);


