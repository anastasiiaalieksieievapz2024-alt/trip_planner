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
    public async Task<PagedResult<TripReadDto>> GetTripsAsync(
        ClaimsPrincipal user,
        TripCollectionQuery query,
        CancellationToken cancellationToken)
    {
        var userId = await ResolvePlannerUserIdAsync(user, cancellationToken);
        if (userId is null)
        {
            return new PagedResult<TripReadDto>(Array.Empty<TripReadDto>(), query.Page, query.PageSize, 0, 0);
        }

        IQueryable<Trip> trips = db.Trips.AsNoTracking()
            .Where(trip => trip.OwnerId == userId.Value || db.TripMembers.Any(member =>
                member.TripId == trip.TripId && member.UserId == userId.Value));

        if (!string.IsNullOrWhiteSpace(query.Destination))
            trips = trips.Where(trip => trip.Destination.Contains(query.Destination));
        if (!string.IsNullOrWhiteSpace(query.Category))
            trips = trips.Where(trip => trip.Category.Name.Contains(query.Category));

        var totalCount = await trips.CountAsync(cancellationToken);
        var descending = query.SortOrder == "desc";
        var orderedTrips = query.SortBy.ToLowerInvariant() switch
        {
            "tripname" => descending ? trips.OrderByDescending(trip => trip.TripName) : trips.OrderBy(trip => trip.TripName),
            "destination" => descending ? trips.OrderByDescending(trip => trip.Destination) : trips.OrderBy(trip => trip.Destination),
            "enddate" => descending ? trips.OrderByDescending(trip => trip.EndDate) : trips.OrderBy(trip => trip.EndDate),
            "category" => descending ? trips.OrderByDescending(trip => trip.Category.Name) : trips.OrderBy(trip => trip.Category.Name),
            _ => descending ? trips.OrderByDescending(trip => trip.StartDate) : trips.OrderBy(trip => trip.StartDate)
        };

        var items = await orderedTrips.ThenBy(trip => trip.TripId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
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
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<TripReadDto>(items, query.Page, query.PageSize, totalCount, totalPages);
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

    public async Task<PagedResult<ItineraryItemReadDto>?> GetItineraryAsync(
        ClaimsPrincipal user,
        int tripId,
        ItineraryCollectionQuery query,
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

        IQueryable<ItineraryItem> itinerary = db.ItineraryItems.AsNoTracking()
            .Where(item => item.TripId == tripId);
        if (!string.IsNullOrWhiteSpace(query.Title)) itinerary = itinerary.Where(item => item.Title.Contains(query.Title));
        if (!string.IsNullOrWhiteSpace(query.Type)) itinerary = itinerary.Where(item => item.Type.Name.Contains(query.Type));
        if (query.From.HasValue) itinerary = itinerary.Where(item => item.StartDateTime >= query.From.Value);
        if (query.To.HasValue) itinerary = itinerary.Where(item => item.StartDateTime <= query.To.Value);

        var totalCount = await itinerary.CountAsync(cancellationToken);
        var descending = query.SortOrder == "desc";
        var orderedItems = query.SortBy.ToLowerInvariant() switch
        {
            "enddatetime" => descending ? itinerary.OrderByDescending(item => item.EndDateTime) : itinerary.OrderBy(item => item.EndDateTime),
            "title" => descending ? itinerary.OrderByDescending(item => item.Title) : itinerary.OrderBy(item => item.Title),
            "type" => descending ? itinerary.OrderByDescending(item => item.Type.Name) : itinerary.OrderBy(item => item.Type.Name),
            _ => descending ? itinerary.OrderByDescending(item => item.StartDateTime) : itinerary.OrderBy(item => item.StartDateTime)
        };

        var stops = await orderedItems.ThenBy(item => item.ItemId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
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

        var items = stops.Select(stop => new ItineraryItemReadDto(
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
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<ItineraryItemReadDto>(items, query.Page, query.PageSize, totalCount, totalPages);
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


