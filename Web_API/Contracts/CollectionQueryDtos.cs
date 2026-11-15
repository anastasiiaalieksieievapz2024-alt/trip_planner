using System.ComponentModel.DataAnnotations;

namespace Web_API.Contracts;

/// <summary>Спільні налаштування сторінки для запитів, які повертають колекції.</summary>
public abstract class PaginationQuery : IValidatableObject
{
    [Range(1, 1000000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 10;
    public string SortDirection { get; init; } = "asc";

    protected abstract IReadOnlySet<string> AllowedSortFields { get; }

    public string SortOrder => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
    public abstract string SortBy { get; init; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("SortDirection має бути asc або desc.", [nameof(SortDirection)]);

        if (!AllowedSortFields.Contains(SortBy))
            yield return new ValidationResult(
                $"Непідтримуване поле сортування. Доступні поля: {string.Join(", ", AllowedSortFields)}.",
                ["sortBy"]);
    }
}

/// <summary>Фільтри та сортування списку поїздок.</summary>
public sealed class TripCollectionQuery : PaginationQuery
{
    public string? Destination { get; init; }
    public string? Category { get; init; }
    public override string SortBy { get; init; } = "startDate";
    protected override IReadOnlySet<string> AllowedSortFields { get; } =
        new HashSet<string>(["startDate", "tripName", "destination", "endDate", "category"], StringComparer.OrdinalIgnoreCase);
}

/// <summary>Фільтри та сортування списку пунктів маршруту.</summary>
public sealed class ItineraryCollectionQuery : PaginationQuery
{
    public string? Title { get; init; }
    public string? Type { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public override string SortBy { get; init; } = "startDateTime";
    protected override IReadOnlySet<string> AllowedSortFields { get; } =
        new HashSet<string>(["startDateTime", "endDateTime", "title", "type"], StringComparer.OrdinalIgnoreCase);

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in base.Validate(validationContext)) yield return error;
        if (From.HasValue && To.HasValue && To < From)
            yield return new ValidationResult("Параметр to має бути не раніше from.", [nameof(To)]);
    }
}

/// <summary>Фільтри та сортування списку учасників поїздки.</summary>
public sealed class TripMemberCollectionQuery : PaginationQuery
{
    public string? Search { get; init; }
    public string? Role { get; init; }
    public override string SortBy { get; init; } = "name";
    protected override IReadOnlySet<string> AllowedSortFields { get; } =
        new HashSet<string>(["name", "email", "role", "joinedAt"], StringComparer.OrdinalIgnoreCase);
}

/// <summary>Колекція на одній сторінці та її метадані.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages)
{
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
