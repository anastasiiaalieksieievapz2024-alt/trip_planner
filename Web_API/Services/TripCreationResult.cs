using Web_API.Contracts;

namespace Web_API.Services;

public enum TripCreationStatus
{
    Created,
    PlannerAccountNotFound,
    CategoryNotFound
}

public sealed record TripCreationResult(TripCreationStatus Status, TripReadDto? Trip);

public enum TripMutationStatus
{
    Updated,
    PlannerAccountNotFound,
    TripNotFound,
    CategoryNotFound
}

public sealed record TripMutationResult(TripMutationStatus Status, TripReadDto? Trip);


