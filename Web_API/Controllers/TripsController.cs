using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Web_API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web_API.Controllers;

/// <summary>Read-only trip planner endpoints protected by the API's Auth0 access-token policy.</summary>
[ApiController]
[Route("api/trips")]
[Authorize(Policy = AuthorizationPolicies.ReadTrips)]
public sealed class TripsController(KpzDbContext db, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Lists trips owned by or shared with the authenticated planner user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TripListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TripListItem>>> GetTrips(CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(cancellationToken);
        if (userId is null)
        {
            return Ok(Array.Empty<TripListItem>());
        }

        var trips = await db.Trips.AsNoTracking()
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

        return Ok(trips);
    }

    /// <summary>Gets the chronological route and booking confirmations for a trip the user can access.</summary>
    [HttpGet("{tripId:int}/itinerary")]
    [ProducesResponseType(typeof(IReadOnlyList<RouteStop>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RouteStop>>> GetItinerary(
        int tripId,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(cancellationToken);
        if (userId is null)
        {
            return NotFound();
        }

        var trip = await db.Trips.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TripId == tripId, cancellationToken);
        if (trip is null)
        {
            return NotFound();
        }

        var authorization = await authorizationService.AuthorizeAsync(
            User,
            trip,
            new ReadTripRequirement());
        if (!authorization.Succeeded)
        {
            return NotFound();
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
        return Ok(stops.Select(stop => new RouteStop(
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
        ).ToList());
    }

    private async Task<int?> ResolvePlannerUserIdAsync(CancellationToken cancellationToken)
    {
        var email = PlannerClaims.GetEmail(User);
        if (email is null)
        {
            return null;
        }

        return await db.Users.AsNoTracking()
            .Where(user => user.Email == email)
            .Select(user => (int?)user.UserId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public sealed record TripListItem(
        int TripId,
        string TripName,
        string Destination,
        DateOnly StartDate,
        DateOnly EndDate,
        string? Description,
        string Category);

    public sealed record BookingConfirmation(
        int ItemId,
        string BookingNumber,
        string Provider,
        string Status,
        decimal Price,
        string? ConfirmationDetails);

    public sealed record RouteStop(
        int ItemId,
        string Type,
        string Title,
        string? Description,
        DateTime StartDateTime,
        DateTime EndDateTime,
        string? Address,
        decimal? Latitude,
        decimal? Longitude,
        IReadOnlyList<BookingConfirmation> Bookings);

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
