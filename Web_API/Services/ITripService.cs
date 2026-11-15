using System.Security.Claims;
using Web_API.Contracts;

namespace Web_API.Services;

/// <summary>Виконує бізнес-операції з поїздками для API.</summary>
public interface ITripService
{
    Task<IReadOnlyList<TripReadDto>> GetTripsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken);

    Task<TripReadDto?> GetTripAsync(
        ClaimsPrincipal user,
        int tripId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ItineraryItemReadDto>?> GetItineraryAsync(
        ClaimsPrincipal user,
        int tripId,
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


