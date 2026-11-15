using System.ComponentModel.DataAnnotations;

namespace Web_API.Contracts;

/// <summary>Запит на додавання зареєстрованого користувача до поїздки як читача.</summary>
public sealed class AddTripMemberDto
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
}

/// <summary>Дані учасника поїздки.</summary>
public sealed record TripMemberReadDto(int UserId, string FullName, string Email, string Role, DateTime JoinedAt);


