using System.Security.Claims;
using Web_API.Contracts;

namespace Web_API.Services;

/// <summary>Виконує CRUD-операції над пунктами маршруту.</summary>
public interface IItineraryItemService
{
    Task<IReadOnlyList<ItineraryItemReadDto>?> GetItemsAsync(ClaimsPrincipal user, int tripId, CancellationToken cancellationToken);
    Task<ItineraryItemReadDto?> GetItemAsync(ClaimsPrincipal user, int tripId, int itemId, CancellationToken cancellationToken);
    Task<ItineraryItemMutationResult> CreateAsync(ClaimsPrincipal user, int tripId, CreateItineraryItemDto request, CancellationToken cancellationToken);
    Task<ItineraryItemMutationResult> UpdateAsync(ClaimsPrincipal user, int tripId, int itemId, UpdateItineraryItemDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(ClaimsPrincipal user, int tripId, int itemId, CancellationToken cancellationToken);
}


