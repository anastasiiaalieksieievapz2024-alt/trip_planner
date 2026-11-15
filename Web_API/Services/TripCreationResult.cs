using Web_API.Contracts;

namespace Web_API.Services;

public enum TripCreationStatus
{
    Created,
    PlannerAccountNotFound,
    CategoryNotFound
}

public sealed record TripCreationResult(TripCreationStatus Status, TripCreatedResponse? Trip);
