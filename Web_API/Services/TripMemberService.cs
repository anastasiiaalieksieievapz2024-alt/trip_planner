using System.Security.Claims;
using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Web_API.Authorization;
using Web_API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Web_API.Mappings;

namespace Web_API.Services;

/// <summary>Керує членством користувачів у поїздках і перевіряє права власника.</summary>
public sealed class TripMemberService(KpzDbContext db, IAuthorizationService authorizationService) : ITripMemberService
{
    public async Task<IReadOnlyList<TripMemberReadDto>?> GetMembersAsync(
        ClaimsPrincipal user, int tripId, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TripId == tripId, cancellationToken);
        if (trip is null || !(await authorizationService.AuthorizeAsync(user, trip, new ReadTripRequirement())).Succeeded)
        {
            return null;
        }

        return await db.TripMembers.AsNoTracking()
            .Where(member => member.TripId == tripId)
            .OrderBy(member => member.User.FullName)
            .Select(member => new TripMemberReadDto(
                member.UserId, member.User.FullName, member.User.Email, member.Role, member.JoinedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TripMemberMutationResult> AddMemberAsync(
        ClaimsPrincipal user, int tripId, string email, CancellationToken cancellationToken)
    {
        var trip = await GetOwnedTripAsync(user, tripId, cancellationToken);
        if (trip is null)
        {
            return new(TripMemberMutationStatus.TripNotFound, null);
        }

        var memberUser = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email, cancellationToken);
        if (memberUser is null)
        {
            return new(TripMemberMutationStatus.UserNotFound, null);
        }

        var alreadyMember = await db.TripMembers.AnyAsync(
            member => member.TripId == tripId && member.UserId == memberUser.UserId, cancellationToken);
        if (alreadyMember)
        {
            return new(TripMemberMutationStatus.AlreadyMember, null);
        }

        var membership = new TripMember
        {
            TripId = tripId,
            UserId = memberUser.UserId,
            Role = "TripReader"
        };
        db.TripMembers.Add(membership);
        await db.SaveChangesAsync(cancellationToken);

        membership.User = memberUser;
        return new(TripMemberMutationStatus.Added, DomainDtoMapper.ToReadDto(membership));
    }

    public async Task<bool> DeleteMemberAsync(
        ClaimsPrincipal user, int tripId, int userId, CancellationToken cancellationToken)
    {
        var trip = await GetOwnedTripAsync(user, tripId, cancellationToken);
        if (trip is null)
        {
            return false;
        }

        var membership = await db.TripMembers.SingleOrDefaultAsync(
            member => member.TripId == tripId && member.UserId == userId, cancellationToken);
        if (membership is null)
        {
            return false;
        }

        db.TripMembers.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Trip?> GetOwnedTripAsync(
        ClaimsPrincipal user, int tripId, CancellationToken cancellationToken)
    {
        var email = PlannerClaims.GetEmail(user);
        if (email is null)
        {
            return null;
        }

        var userId = await db.Users.AsNoTracking()
            .Where(candidate => candidate.Email == email)
            .Select(candidate => (int?)candidate.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (userId is null)
        {
            return null;
        }

        return await db.Trips.SingleOrDefaultAsync(
            trip => trip.TripId == tripId && trip.OwnerId == userId.Value, cancellationToken);
    }
}


