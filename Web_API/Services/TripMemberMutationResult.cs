using Web_API.Contracts;

namespace Web_API.Services;

public enum TripMemberMutationStatus
{
    Added,
    TripNotFound,
    UserNotFound,
    AlreadyMember
}

public sealed record TripMemberMutationResult(TripMemberMutationStatus Status, TripMemberReadDto? Member);


