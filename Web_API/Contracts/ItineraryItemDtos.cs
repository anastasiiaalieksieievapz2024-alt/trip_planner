using System.ComponentModel.DataAnnotations;
using Web_API.Validation;

namespace Web_API.Contracts;

/// <summary>Спільні поля запиту для створення та оновлення пункту маршруту.</summary>
public abstract class ItineraryItemWriteDto : IValidatableObject
{
    [Range(1, int.MaxValue)] public int ItineraryItemTypeId { get; init; }
    [Required, NotWhiteSpace, StringLength(200)] public string Title { get; init; } = string.Empty;
    [StringLength(2000)] public string? Description { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    [StringLength(400)] public string? Address { get; init; }
    [Range(typeof(decimal), "-90", "90")] public decimal? Latitude { get; init; }
    [Range(typeof(decimal), "-180", "180")] public decimal? Longitude { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDateTime == default)
            yield return new ValidationResult("Вкажіть дату й час початку.", [nameof(StartDateTime)]);
        if (EndDateTime == default)
            yield return new ValidationResult("Вкажіть дату й час завершення.", [nameof(EndDateTime)]);
        if (StartDateTime != default && EndDateTime != default && EndDateTime < StartDateTime)
            yield return new ValidationResult("Час завершення має бути не раніше часу початку.", [nameof(EndDateTime)]);
    }
}

/// <summary>Дані для створення пункту маршруту.</summary>
public sealed class CreateItineraryItemDto : ItineraryItemWriteDto { }

/// <summary>Дані для повного оновлення пункту маршруту.</summary>
public sealed class UpdateItineraryItemDto : ItineraryItemWriteDto { }

/// <summary>Пункт маршруту для читання разом із підтвердженнями бронювань.</summary>
public sealed record ItineraryItemReadDto(int ItemId, string Type, string Title, string? Description,
    DateTime StartDateTime, DateTime EndDateTime, string? Address, decimal? Latitude,
    decimal? Longitude, IReadOnlyList<BookingConfirmation> Bookings);
