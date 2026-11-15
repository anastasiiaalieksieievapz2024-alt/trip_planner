using Web_API.Contracts;

namespace Web_API.Services;

public enum ItineraryItemMutationStatus
{
    Succeeded,
    TripNotFound,
    ItemNotFound,
    TypeNotFound,
    OutsideTripDates
}

public sealed record ItineraryItemMutationResult(ItineraryItemMutationStatus Status, ItineraryItemReadDto? Item);


