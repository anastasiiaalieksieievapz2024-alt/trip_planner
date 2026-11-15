using System.ComponentModel.DataAnnotations;
using Web_API.Validation;

namespace Web_API.Contracts;

/// <summary>Спільні поля запиту для створення та повного оновлення поїздки.</summary>
public abstract class TripWriteDto : IValidatableObject
{
    [Required, NotWhiteSpace, StringLength(150)] public string TripName { get; init; } = string.Empty;
    [Required, NotWhiteSpace, StringLength(200)] public string Destination { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int TripCategoryId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    [StringLength(2000)] public string? Description { get; init; }
    [StringLength(1000)] public string? PlanningNotes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == default)
            yield return new ValidationResult("Вкажіть дату початку поїздки.", [nameof(StartDate)]);
        if (EndDate == default)
            yield return new ValidationResult("Вкажіть дату завершення поїздки.", [nameof(EndDate)]);
        if (StartDate != default && EndDate != default && EndDate < StartDate)
            yield return new ValidationResult("Дата завершення має бути не раніше дати початку.", [nameof(EndDate)]);
    }
}

/// <summary>Дані для створення поїздки.</summary>
public sealed class CreateTripDto : TripWriteDto { }

/// <summary>Дані для повного оновлення поїздки.</summary>
public sealed class UpdateTripDto : TripWriteDto { }

/// <summary>Дані поїздки, які повертає API.</summary>
public sealed record TripReadDto(int TripId, string TripName, string Destination, DateOnly StartDate,
    DateOnly EndDate, string? Description, int TripCategoryId, string Category);
