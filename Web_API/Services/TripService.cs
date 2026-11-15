using System.Security.Claims;
using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Web_API.Authorization;
using Web_API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Web_API.Mappings;

namespace Web_API.Services;

/// <summary>Реалізує бізнес-правила читання, авторизації та створення поїздок.</summary>
public sealed class TripService(KpzDbContext db, IAuthorizationService authorizationService) : ITripService
{
    public async Task<IReadOnlyList<TripReadDto>> GetTripsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(user, cancellationToken);
        if (userId is null)
        {
            return Array.Empty<TripReadDto>();
        }

        return await db.Trips.AsNoTracking()
            .Where(trip => trip.OwnerId == userId.Value || db.TripMembers.Any(member =>
                member.TripId == trip.TripId && member.UserId == userId.Value))
            .OrderBy(trip => trip.StartDate)
            .ThenBy(trip => trip.TripId)
            .Select(trip => new TripReadDto(
                trip.TripId,
                trip.TripName,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                trip.Description,
                trip.TripCategoryId,
                trip.Category.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<TripReadDto?> GetTripAsync(
        ClaimsPrincipal user,
        int tripId,
        CancellationToken cancellationToken)
    {
        var trip = await db.Trips.AsNoTracking()
            .Include(candidate => candidate.Category)
            .SingleOrDefaultAsync(candidate => candidate.TripId == tripId, cancellationToken);
        if (trip is null || !await CanReadTripAsync(user, trip, cancellationToken))
        {
            return null;
        }

        return DomainDtoMapper.ToReadDto(trip);
    }

    public async Task<IReadOnlyList<ItineraryItemReadDto>?> GetItineraryAsync(
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

        return stops.Select(stop => new ItineraryItemReadDto(
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
        CreateTripDto request,
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

        var trip = new Trip { OwnerId = userId.Value, TripName = string.Empty, Destination = string.Empty };
        DomainDtoMapper.Apply(request, trip);

        db.Trips.Add(trip);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(trip).Reference(candidate => candidate.Category).LoadAsync(cancellationToken);
        var response = DomainDtoMapper.ToReadDto(trip);
        return new TripCreationResult(TripCreationStatus.Created, response);
    }

    public async Task<TripMutationResult> UpdateTripAsync(
        ClaimsPrincipal user,
        int tripId,
        UpdateTripDto request,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(user, cancellationToken);
        if (userId is null)
        {
            return new TripMutationResult(TripMutationStatus.PlannerAccountNotFound, null);
        }

        var trip = await db.Trips.SingleOrDefaultAsync(candidate => candidate.TripId == tripId, cancellationToken);
        if (trip is null || trip.OwnerId != userId.Value)
        {
            return new TripMutationResult(TripMutationStatus.TripNotFound, null);
        }

        var categoryExists = await db.TripCategories.AsNoTracking()
            .AnyAsync(category => category.TripCategoryId == request.TripCategoryId, cancellationToken);
        if (!categoryExists)
        {
            return new TripMutationResult(TripMutationStatus.CategoryNotFound, null);
        }

        DomainDtoMapper.Apply(request, trip);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(trip).Reference(candidate => candidate.Category).LoadAsync(cancellationToken);

        return new TripMutationResult(TripMutationStatus.Updated, DomainDtoMapper.ToReadDto(trip));
    }

    public async Task<bool> DeleteTripAsync(
        ClaimsPrincipal user,
        int tripId,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(user, cancellationToken);
        if (userId is null)
        {
            return false;
        }

        var trip = await db.Trips.SingleOrDefaultAsync(candidate => candidate.TripId == tripId, cancellationToken);
        if (trip is null || trip.OwnerId != userId.Value)
        {
            return false;
        }

        db.Trips.Remove(trip);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> CanReadTripAsync(ClaimsPrincipal user, Trip trip, CancellationToken cancellationToken)
    {
        if (await ResolvePlannerUserIdAsync(user, cancellationToken) is null)
        {
            return false;
        }

        var authorization = await authorizationService.AuthorizeAsync(user, trip, new ReadTripRequirement());
        return authorization.Succeeded;
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


