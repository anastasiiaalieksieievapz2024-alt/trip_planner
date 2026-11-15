using System.ComponentModel.DataAnnotations;

namespace Web_API.Contracts;

/// <summary>JSON-поля, потрібні для створення поїздки.</summary>
public sealed class CreateTripRequest : IValidatableObject
{
    /// <summary>Назва поїздки (до 150 символів).</summary>
    [Required, StringLength(150)]
    public string TripName { get; init; } = string.Empty;

    /// <summary>Місце призначення (до 200 символів).</summary>
    [Required, StringLength(200)]
    public string Destination { get; init; } = string.Empty;

    /// <summary>Ідентифікатор категорії, що існує в каталозі категорій.</summary>
    [Range(1, int.MaxValue)]
    public int TripCategoryId { get; init; }

    /// <summary>Дата початку поїздки у форматі YYYY-MM-DD.</summary>
    public DateOnly StartDate { get; init; }

    /// <summary>Дата завершення поїздки у форматі YYYY-MM-DD.</summary>
    public DateOnly EndDate { get; init; }

    /// <summary>Необов'язковий опис поїздки (до 2000 символів).</summary>
    [StringLength(2000)]
    public string? Description { get; init; }

    /// <summary>Необов'язкові нотатки планування (до 1000 символів).</summary>
    [StringLength(1000)]
    public string? PlanningNotes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == default)
        {
            yield return new ValidationResult("Вкажіть дату початку поїздки.", [nameof(StartDate)]);
        }

        if (EndDate == default)
        {
            yield return new ValidationResult("Вкажіть дату завершення поїздки.", [nameof(EndDate)]);
        }

        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "Дата завершення має бути не раніше дати початку.",
                [nameof(EndDate)]);
        }
    }
}
