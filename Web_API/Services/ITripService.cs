using System.Security.Claims;
using Web_API.Contracts;

namespace Web_API.Services;

/// <summary>Виконує бізнес-операції з поїздками для API.</summary>
public interface ITripService
{
    Task<PagedResult<TripReadDto>> GetTripsAsync(
        ClaimsPrincipal user,
        TripCollectionQuery query,
        CancellationToken cancellationToken);

    Task<TripReadDto?> GetTripAsync(
        ClaimsPrincipal user,
        int tripId,
        CancellationToken cancellationToken);

    Task<PagedResult<ItineraryItemReadDto>?> GetItineraryAsync(
        ClaimsPrincipal user,
        int tripId,
        ItineraryCollectionQuery query,
        CancellationToken cancellationToken);

    Task<TripCreationResult> CreateTripAsync(
        ClaimsPrincipal user,
        CreateTripDto request,
        CancellationToken cancellationToken);

    Task<TripMutationResult> UpdateTripAsync(
        ClaimsPrincipal user,
        int tripId,
        UpdateTripDto request,
        CancellationToken cancellationToken);

    Task<bool> DeleteTripAsync(
        ClaimsPrincipal user,
        int tripId,
        CancellationToken cancellationToken);
}


