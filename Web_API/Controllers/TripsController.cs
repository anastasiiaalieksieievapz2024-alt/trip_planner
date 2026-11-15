using Web_API.Authorization;
using Web_API.Contracts;
using Web_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web_API.Controllers;

/// <summary>Кінцеві точки для читання та створення поїздок.</summary>
[ApiController]
[Route("api/trips")]
[Produces("application/json")]
public sealed class TripsController(ITripService tripService) : ControllerBase
{
    /// <summary>Повертає поїздки, якими володіє користувач або до яких його додано як учасника.</summary>
    /// <param name="cancellationToken">Токен для скасування запиту до сервісу.</param>
    /// <response code="200">Повертає доступні користувачу поїздки або порожній масив.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен не містить дозволу <c>read:trips</c>.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadTrips)]
    [ProducesResponseType<IReadOnlyList<TripListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу read:trips.")]
    public async Task<ActionResult<IReadOnlyList<TripListItem>>> GetTrips(CancellationToken cancellationToken)
    {
        var trips = await tripService.GetTripsAsync(User, cancellationToken);
        return Ok(trips);
    }

    /// <summary>Повертає хронологічний маршрут і підтвердження бронювань, якщо користувач має доступ до поїздки.</summary>
    /// <param name="tripId">Ідентифікатор запитаної поїздки.</param>
    /// <param name="cancellationToken">Токен для скасування запиту до сервісу.</param>
    /// <response code="200">Повертає маршрут поїздки та підтвердження бронювань.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен не містить дозволу <c>read:trips</c>.</response>
    /// <response code="404">Поїздку не знайдено або користувач не має до неї доступу.</response>
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
        var itinerary = await tripService.GetItineraryAsync(User, tripId, cancellationToken);
        return itinerary is null ? NotFound() : Ok(itinerary);
    }

    /// <summary>Створює поїздку від імені автентифікованого користувача з дозволом <c>create:trips</c>.</summary>
    /// <param name="request">Поля нової поїздки у форматі JSON.</param>
    /// <param name="cancellationToken">Токен для скасування запиту до сервісу.</param>
    /// <response code="201">Поїздку створено; у відповіді повертаються її ідентифікатор та дані.</response>
    /// <response code="400">JSON або дані поїздки не пройшли перевірку, або категорія не існує.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен не містить дозволу <c>create:trips</c>.</response>
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
        var result = await tripService.CreateTripAsync(User, request, cancellationToken);
        if (result.Status == TripCreationStatus.PlannerAccountNotFound)
        {
            return Forbid();
        }

        if (result.Status == TripCreationStatus.CategoryNotFound)
        {
            ModelState.AddModelError(nameof(request.TripCategoryId), "Категорію поїздки не знайдено.");
            return ValidationProblem(ModelState);
        }

        var trip = result.Trip!;
        return CreatedAtAction(nameof(GetItinerary), new { tripId = trip.TripId }, trip);
    }
}
