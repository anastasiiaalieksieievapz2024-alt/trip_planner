using System.Security.Claims;
using Web_API.Contracts;

namespace Web_API.Services;

/// <summary>Виконує бізнес-операції з поїздками для API.</summary>
public interface ITripService
{
    Task<IReadOnlyList<TripListItem>> GetTripsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RouteStop>?> GetItineraryAsync(
        ClaimsPrincipal user,
        int tripId,
        CancellationToken cancellationToken);

    Task<TripCreationResult> CreateTripAsync(
        ClaimsPrincipal user,
        CreateTripRequest request,
        CancellationToken cancellationToken);
}
