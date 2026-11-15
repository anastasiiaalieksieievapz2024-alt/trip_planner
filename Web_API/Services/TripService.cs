using System.Security.Claims;
using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Web_API.Authorization;
using Web_API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Web_API.Services;

/// <summary>Реалізує бізнес-правила читання, авторизації та створення поїздок.</summary>
public sealed class TripService(KpzDbContext db, IAuthorizationService authorizationService) : ITripService
{
    public async Task<IReadOnlyList<TripListItem>> GetTripsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(user, cancellationToken);
        if (userId is null)
        {
            return Array.Empty<TripListItem>();
        }

        return await db.Trips.AsNoTracking()
            .Where(trip => trip.OwnerId == userId.Value || db.TripMembers.Any(member =>
                member.TripId == trip.TripId && member.UserId == userId.Value))
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId)
            .Select(trip => new TripListItem(
                trip.TripId,
                trip.TripName,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                trip.Description,
                trip.Category.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RouteStop>?> GetItineraryAsync(
        ClaimsPrincipal user,
        int tripId,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(user, cancellationToken);
        if (userId is null)
        {
            return null;
        }

        var trip = await db.Trips.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TripId == tripId, cancellationToken);
        if (trip is null)
        {
            return null;
        }

        var authorization = await authorizationService.AuthorizeAsync(
            user,
            trip,
            new ReadTripRequirement());
        if (!authorization.Succeeded)
        {
            return null;
        }

        var stops = await db.ItineraryItems.AsNoTracking()
            .Where(item => item.TripId == tripId)
            .OrderBy(item => item.StartDateTime)
            .ThenBy(item => item.ItemId)
            .Select(item => new StopData(
                item.ItemId,
                item.Type.Name,
                item.Title,
                item.Description,
                item.StartDateTime,
                item.EndDateTime,
                item.Address,
                item.Latitude,
                item.Longitude))
            .ToListAsync(cancellationToken);

        var itemIds = stops.Select(stop => stop.ItemId).ToArray();
        var bookings = await db.Bookings.AsNoTracking()
            .Where(booking => itemIds.Contains(booking.ItemId))
            .OrderBy(booking => booking.BookingNumber)
            .Select(booking => new BookingConfirmation(
                booking.ItemId,
                booking.BookingNumber,
                booking.Provider,
                booking.Status,
                booking.Price,
                booking.ConfirmationDetails))
            .ToListAsync(cancellationToken);

        var bookingsByItem = bookings.GroupBy(booking => booking.ItemId)
            .ToDictionary(group => group.Key,
                group => (IReadOnlyList<BookingConfirmation>)group.ToList());

        return stops.Select(stop => new RouteStop(
            stop.ItemId,
            stop.Type,
            stop.Title,
            stop.Description,
            stop.StartDateTime,
            stop.EndDateTime,
            stop.Address,
            stop.Latitude,
            stop.Longitude,
            bookingsByItem.GetValueOrDefault(stop.ItemId, Array.Empty<BookingConfirmation>()))
        ).ToList();
    }

    public async Task<TripCreationResult> CreateTripAsync(
        ClaimsPrincipal user,
        CreateTripRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(user, cancellationToken);
        if (userId is null)
        {
            return new TripCreationResult(TripCreationStatus.PlannerAccountNotFound, null);
        }

        var categoryExists = await db.TripCategories.AsNoTracking()
            .AnyAsync(category => category.TripCategoryId == request.TripCategoryId, cancellationToken);
        if (!categoryExists)
        {
            return new TripCreationResult(TripCreationStatus.CategoryNotFound, null);
        }

        var trip = new Trip
        {
            OwnerId = userId.Value,
            TripCategoryId = request.TripCategoryId,
            TripName = request.TripName,
            Destination = request.Destination,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            PlanningNotes = request.PlanningNotes
        };

        db.Trips.Add(trip);
        await db.SaveChangesAsync(cancellationToken);

        var response = new TripCreatedResponse(
            trip.TripId,
            trip.TripName,
            trip.Destination,
            trip.StartDate,
            trip.EndDate,
            trip.Description,
            trip.TripCategoryId);
        return new TripCreationResult(TripCreationStatus.Created, response);
    }

    private async Task<int?> ResolvePlannerUserIdAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var email = PlannerClaims.GetEmail(user);
        if (email is null)
        {
            return null;
        }

        return await db.Users.AsNoTracking()
            .Where(plannerUser => plannerUser.Email == email)
            .Select(plannerUser => (int?)plannerUser.UserId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private sealed record StopData(
        int ItemId,
        string Type,
        string Title,
        string? Description,
        DateTime StartDateTime,
        DateTime EndDateTime,
        string? Address,
        decimal? Latitude,
        decimal? Longitude);
}
