using System.ComponentModel.DataAnnotations;

namespace Web_API.Contracts;

/// <summary>Поля, які клієнт може задати під час створення або повного оновлення поїздки.</summary>
public abstract class TripWriteDto : IValidatableObject
{
    [Required, StringLength(150)] public string TripName { get; init; } = string.Empty;
    [Required, StringLength(200)] public string Destination { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int TripCategoryId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    [StringLength(2000)] public string? Description { get; init; }
    [StringLength(1000)] public string? PlanningNotes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == default) yield return new ValidationResult("Вкажіть дату початку поїздки.", [nameof(StartDate)]);
        if (EndDate == default) yield return new ValidationResult("Вкажіть дату завершення поїздки.", [nameof(EndDate)]);
        if (EndDate < StartDate) yield return new ValidationResult("Дата завершення має бути не раніше дати початку.", [nameof(EndDate)]);
    }
}

/// <summary>Вхідні дані створення поїздки.</summary>
public sealed class CreateTripDto : TripWriteDto { }

/// <summary>Повний набір змінюваних полів під час оновлення поїздки.</summary>
public sealed class UpdateTripDto : TripWriteDto { }

/// <summary>Дані поїздки, які повертаються API.</summary>
public sealed record TripReadDto(int TripId, string TripName, string Destination, DateOnly StartDate,
    DateOnly EndDate, string? Description, int TripCategoryId, string Category);


