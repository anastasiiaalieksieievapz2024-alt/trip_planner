using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Web_API.Authorization;

/// <summary>Allows reading a trip when the planner user owns it or is a member.</summary>
public sealed class ReadTripHandler(KpzDbContext db)
    : AuthorizationHandler<ReadTripRequirement, Trip>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ReadTripRequirement requirement,
        Trip trip)
    {
        var email = PlannerClaims.GetEmail(context.User);
        if (email is null)
        {
            return;
        }

        var userId = await db.Users.AsNoTracking()
            .Where(user => user.Email == email)
            .Select(user => (int?)user.UserId)
            .SingleOrDefaultAsync();
        if (userId is null)
        {
            return;
        }

        var isOwner = trip.OwnerId == userId.Value;
        var isMember = await db.TripMembers.AsNoTracking()
            .AnyAsync(member => member.TripId == trip.TripId && member.UserId == userId.Value);

        if (isOwner || isMember)
        {
            context.Succeed(requirement);
        }
    }
}


