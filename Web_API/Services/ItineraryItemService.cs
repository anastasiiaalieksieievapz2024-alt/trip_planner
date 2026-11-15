using System.Security.Claims;
using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Web_API.Authorization;
using Web_API.Contracts;
using Microsoft.EntityFrameworkCore;
using Web_API.Mappings;
using Web_API.BusinessRules;

namespace Web_API.Services;

/// <summary>Реалізує читання й зміну пунктів маршруту з перевіркою доступу до поїздки.</summary>
public sealed class ItineraryItemService(KpzDbContext db) : IItineraryItemService
{
    public async Task<PagedResult<ItineraryItemReadDto>?> GetItemsAsync(
        ClaimsPrincipal user, int tripId, ItineraryCollectionQuery query, CancellationToken cancellationToken)
    {
        if (!await CanReadTripAsync(user, tripId, cancellationToken))
        {
            return null;
        }

        IQueryable<ItineraryItem> itemsQuery = db.ItineraryItems.AsNoTracking()
            .Where(item => item.TripId == tripId);
        if (!string.IsNullOrWhiteSpace(query.Title)) itemsQuery = itemsQuery.Where(item => item.Title.Contains(query.Title));
        if (!string.IsNullOrWhiteSpace(query.Type)) itemsQuery = itemsQuery.Where(item => item.Type.Name.Contains(query.Type));
        if (query.From.HasValue) itemsQuery = itemsQuery.Where(item => item.StartDateTime >= query.From.Value);
        if (query.To.HasValue) itemsQuery = itemsQuery.Where(item => item.StartDateTime <= query.To.Value);

        var totalCount = await itemsQuery.CountAsync(cancellationToken);
        var descending = query.SortOrder == "desc";
        var orderedItems = query.SortBy.ToLowerInvariant() switch
        {
            "enddatetime" => descending ? itemsQuery.OrderByDescending(item => item.EndDateTime) : itemsQuery.OrderBy(item => item.EndDateTime),
            "title" => descending ? itemsQuery.OrderByDescending(item => item.Title) : itemsQuery.OrderBy(item => item.Title),
            "type" => descending ? itemsQuery.OrderByDescending(item => item.Type.Name) : itemsQuery.OrderBy(item => item.Type.Name),
            _ => descending ? itemsQuery.OrderByDescending(item => item.StartDateTime) : itemsQuery.OrderBy(item => item.StartDateTime)
        };
        var stops = await orderedItems.ThenBy(item => item.ItemId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new StopData(
                item.ItemId, item.Type.Name, item.Title, item.Description,
                item.StartDateTime, item.EndDateTime, item.Address, item.Latitude, item.Longitude))
            .ToListAsync(cancellationToken);

        var resultItems = await AddBookingsAsync(stops, cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<ItineraryItemReadDto>(resultItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<ItineraryItemReadDto?> GetItemAsync(
        ClaimsPrincipal user, int tripId, int itemId, CancellationToken cancellationToken)
    {
        if (!await CanReadTripAsync(user, tripId, cancellationToken))
        {
            return null;
        }

        var stop = await db.ItineraryItems.AsNoTracking()
            .Where(item => item.TripId == tripId && item.ItemId == itemId)
            .Select(item => new StopData(
                item.ItemId, item.Type.Name, item.Title, item.Description,
                item.StartDateTime, item.EndDateTime, item.Address, item.Latitude, item.Longitude))
            .SingleOrDefaultAsync(cancellationToken);
        if (stop is null)
        {
            return null;
        }

        return (await AddBookingsAsync([stop], cancellationToken)).Single();
    }

    public async Task<ItineraryItemMutationResult> CreateAsync(
        ClaimsPrincipal user, int tripId, CreateItineraryItemDto request, CancellationToken cancellationToken)
    {
        var trip = await GetOwnedTripAsync(user, tripId, cancellationToken);
        if (trip is null)
        {
            return new(ItineraryItemMutationStatus.TripNotFound, null);
        }

        if (!ItineraryItemDateRule.IsSatisfied(trip, request))
        {
            return new(ItineraryItemMutationStatus.OutsideTripDates, null);
        }

        var typeExists = await db.ItineraryItemTypes.AsNoTracking()
            .AnyAsync(type => type.ItineraryItemTypeId == request.ItineraryItemTypeId, cancellationToken);
        if (!typeExists)
        {
            return new(ItineraryItemMutationStatus.TypeNotFound, null);
        }

        var item = DomainDtoMapper.ToEntity(request, trip.TripId);
        db.ItineraryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        var response = await GetItemAsync(user, tripId, item.ItemId, cancellationToken);
        return new(ItineraryItemMutationStatus.Succeeded, response);
    }

    public async Task<ItineraryItemMutationResult> UpdateAsync(
        ClaimsPrincipal user, int tripId, int itemId, UpdateItineraryItemDto request, CancellationToken cancellationToken)
    {
        var trip = await GetOwnedTripAsync(user, tripId, cancellationToken);
        if (trip is null)
        {
            return new(ItineraryItemMutationStatus.TripNotFound, null);
        }

        var item = await db.ItineraryItems.SingleOrDefaultAsync(
            candidate => candidate.TripId == tripId && candidate.ItemId == itemId, cancellationToken);
        if (item is null)
        {
            return new(ItineraryItemMutationStatus.ItemNotFound, null);
        }

        if (!ItineraryItemDateRule.IsSatisfied(trip, request))
        {
            return new(ItineraryItemMutationStatus.OutsideTripDates, null);
        }

        var typeExists = await db.ItineraryItemTypes.AsNoTracking()
            .AnyAsync(type => type.ItineraryItemTypeId == request.ItineraryItemTypeId, cancellationToken);
        if (!typeExists)
        {
            return new(ItineraryItemMutationStatus.TypeNotFound, null);
        }

        DomainDtoMapper.Apply(request, item);
        await db.SaveChangesAsync(cancellationToken);

        return new(ItineraryItemMutationStatus.Succeeded,
            await GetItemAsync(user, tripId, itemId, cancellationToken));
    }

    public async Task<bool> DeleteAsync(
        ClaimsPrincipal user, int tripId, int itemId, CancellationToken cancellationToken)
    {
        var trip = await GetOwnedTripAsync(user, tripId, cancellationToken);
        if (trip is null)
        {
            return false;
        }

        var item = await db.ItineraryItems.SingleOrDefaultAsync(
            candidate => candidate.TripId == tripId && candidate.ItemId == itemId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        db.ItineraryItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> CanReadTripAsync(ClaimsPrincipal user, int tripId, CancellationToken cancellationToken)
    {
        var email = PlannerClaims.GetEmail(user);
        if (email is null)
        {
            return false;
        }

        return await db.Trips.AsNoTracking().AnyAsync(trip =>
            trip.TripId == tripId &&
            (trip.Owner.Email == email || trip.Members.Any(member => member.User.Email == email)),
            cancellationToken);
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
            .Where(plannerUser => plannerUser.Email == email)
            .Select(plannerUser => (int?)plannerUser.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (userId is null)
        {
            return null;
        }

        return await db.Trips.SingleOrDefaultAsync(
            trip => trip.TripId == tripId && trip.OwnerId == userId.Value, cancellationToken);
    }

    private async Task<IReadOnlyList<ItineraryItemReadDto>> AddBookingsAsync(
        IReadOnlyList<StopData> stops, CancellationToken cancellationToken)
    {
        var itemIds = stops.Select(stop => stop.ItemId).ToArray();
        var bookings = await db.Bookings.AsNoTracking()
            .Where(booking => itemIds.Contains(booking.ItemId))
            .OrderBy(booking => booking.BookingNumber)
            .Select(booking => new BookingConfirmation(
                booking.ItemId, booking.BookingNumber, booking.Provider,
                booking.Status, booking.Price, booking.ConfirmationDetails))
            .ToListAsync(cancellationToken);
        var bookingsByItem = bookings.GroupBy(booking => booking.ItemId)
            .ToDictionary(group => group.Key,
                group => (IReadOnlyList<BookingConfirmation>)group.ToList());

        return stops.Select(stop => new ItineraryItemReadDto(
            stop.ItemId, stop.Type, stop.Title, stop.Description,
            stop.StartDateTime, stop.EndDateTime, stop.Address, stop.Latitude, stop.Longitude,
            bookingsByItem.GetValueOrDefault(stop.ItemId, Array.Empty<BookingConfirmation>()))).ToList();
    }

    private sealed record StopData(
        int ItemId, string Type, string Title, string? Description,
        DateTime StartDateTime, DateTime EndDateTime, string? Address,
        decimal? Latitude, decimal? Longitude);
}


