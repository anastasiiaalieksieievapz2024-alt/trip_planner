using System.Security.Claims;
using Web_API.Contracts;

namespace Web_API.Services;

/// <summary>Виконує операції керування учасниками поїздок.</summary>
public interface ITripMemberService
{
    Task<PagedResult<TripMemberReadDto>?> GetMembersAsync(ClaimsPrincipal user, int tripId, TripMemberCollectionQuery query, CancellationToken cancellationToken);
    Task<TripMemberMutationResult> AddMemberAsync(ClaimsPrincipal user, int tripId, string email, CancellationToken cancellationToken);
    Task<bool> DeleteMemberAsync(ClaimsPrincipal user, int tripId, int userId, CancellationToken cancellationToken);
}


