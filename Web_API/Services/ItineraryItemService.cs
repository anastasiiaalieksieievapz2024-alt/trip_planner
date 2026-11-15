using System.Security.Claims;
using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Web_API.Authorization;
using Web_API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Web_API.Mappings;

namespace Web_API.Services;

/// <summary>Реалізує читання й зміну пунктів маршруту з перевіркою доступу до поїздки.</summary>
public sealed class ItineraryItemService(KpzDbContext db, IAuthorizationService authorizationService)
    : IItineraryItemService
{
    public async Task<IReadOnlyList<ItineraryItemReadDto>?> GetItemsAsync(
        ClaimsPrincipal user, int tripId, CancellationToken cancellationToken)
    {
        if (!await CanReadTripAsync(user, tripId, cancellationToken))
        {
            return null;
        }

        var stops = await db.ItineraryItems.AsNoTracking()
            .Where(item => item.TripId == tripId)
            .OrderBy(item => item.StartDateTime)
            .ThenBy(item => item.ItemId)
            .Select(item => new StopData(
                item.ItemId, item.Type.Name, item.Title, item.Description,
                item.StartDateTime, item.EndDateTime, item.Address, item.Latitude, item.Longitude))
            .ToListAsync(cancellationToken);

        return await AddBookingsAsync(stops, cancellationToken);
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
        var trip = await db.Trips.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TripId == tripId, cancellationToken);
        if (trip is null)
        {
            return false;
        }

        var result = await authorizationService.AuthorizeAsync(user, trip, new ReadTripRequirement());
        return result.Succeeded;
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


