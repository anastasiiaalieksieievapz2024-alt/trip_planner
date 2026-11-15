using System.ComponentModel.DataAnnotations;
using Web_API.Validation;

namespace Web_API.Contracts;

/// <summary>Запит на додавання зареєстрованого користувача до поїздки.</summary>
public sealed class AddTripMemberDto
{
    [Required, NotWhiteSpace, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
}

/// <summary>Дані учасника поїздки, які повертає API.</summary>
public sealed record TripMemberReadDto(int UserId, string FullName, string Email, string Role, DateTime JoinedAt);
