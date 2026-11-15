using Kpz.Lab2.Data;
using Kpz.Lab2.Models;
using Web_API.Authorization;
using Web_API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web_API.Controllers;

/// <summary>Кінцеві точки для читання поїздок, захищені політикою access token Auth0.</summary>
[ApiController]
[Route("api/trips")]
[Produces("application/json")]
public sealed class TripsController(KpzDbContext db, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Повертає поїздки, якими володіє користувач або до яких його додано як учасника.</summary>
    /// <param name="cancellationToken">Токен для скасування запиту до бази даних.</param>
    /// <response code="200">Повертає доступні користувачу поїздки або порожній масив, якщо email токена не зіставлено з обліковим записом.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен не містить дозволу <c>read:trips</c>.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadTrips)]
    [ProducesResponseType<IReadOnlyList<TripListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу read:trips.")]
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

    /// <summary>Повертає хронологічний маршрут і підтвердження бронювань, якщо користувач має доступ до поїздки.</summary>
    /// <param name="tripId">Ідентифікатор запитаної поїздки.</param>
    /// <param name="cancellationToken">Токен для скасування запитів до бази даних.</param>
    /// <response code="200">Повертає маршрут поїздки та підтвердження бронювань.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен не містить дозволу <c>read:trips</c>.</response>
    /// <response code="404">Поїздку не знайдено, email токена не має облікового запису або користувач не є власником чи учасником.</response>
    [HttpGet("{tripId:int}/itinerary")]
    [Authorize(Policy = AuthorizationPolicies.ReadTrips)]
    [ProducesResponseType<IReadOnlyList<RouteStop>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу read:trips.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound, Description = "Поїздку не знайдено або користувач не має до неї доступу.")]
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

    /// <summary>Створює поїздку від імені автентифікованого користувача з дозволом <c>create:trips</c>.</summary>
    /// <param name="request">Поля нової поїздки у форматі JSON.</param>
    /// <param name="cancellationToken">Токен для скасування запиту до бази даних.</param>
    /// <response code="201">Поїздку створено; у відповіді повертається її ідентифікатор та дані.</response>
    /// <response code="400">JSON або дані поїздки не пройшли перевірку, або категорія не існує.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">У токені немає дозволу <c>create:trips</c>.</response>
    /// <response code="415">Тіло запиту має бути у форматі JSON.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CreateTrips)]
    [Consumes("application/json")]
    [ProducesResponseType<TripCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу create:trips.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType, Description = "Надішліть тіло запиту з Content-Type: application/json.")]
    public async Task<ActionResult<TripCreatedResponse>> CreateTrip(
        [FromBody] CreateTripRequest request,
        CancellationToken cancellationToken)
    {
        var email = PlannerClaims.GetEmail(User);
        if (email is null)
        {
            return Forbid();
        }

        var userId = await db.Users.AsNoTracking()
            .Where(user => user.Email == email)
            .Select(user => (int?)user.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (userId is null)
        {
            return Forbid();
        }

        var categoryExists = await db.TripCategories.AsNoTracking()
            .AnyAsync(category => category.TripCategoryId == request.TripCategoryId, cancellationToken);
        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(request.TripCategoryId), "Категорію поїздки не знайдено.");
            return ValidationProblem(ModelState);
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

        return CreatedAtAction(
            nameof(GetItinerary),
            new { tripId = trip.TripId },
            new TripCreatedResponse(
                trip.TripId,
                trip.TripName,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                trip.Description,
                trip.TripCategoryId));
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
