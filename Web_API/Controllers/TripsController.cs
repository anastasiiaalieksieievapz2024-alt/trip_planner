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
    /// <param name="query">Параметри сторінки, фільтрації та сортування.</param>
    /// <param name="cancellationToken">Токен для скасування запиту до сервісу.</param>
    /// <response code="200">Повертає доступні користувачу поїздки або порожній масив.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен не містить дозволу <c>read:trips</c>.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadTrips)]
    [ProducesResponseType<PagedResult<TripReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу read:trips.")]
    public async Task<ActionResult<PagedResult<TripReadDto>>> GetTrips(
        [FromQuery] TripCollectionQuery query, CancellationToken cancellationToken)
    {
        var trips = await tripService.GetTripsAsync(User, query, cancellationToken);
        return Ok(trips);
    }

    /// <summary>Повертає деталі поїздки для власника або учасника.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="200">Поїздку знайдено.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу read:trips.</response>
    /// <response code="404">Поїздки немає або користувач не має доступу.</response>
    [HttpGet("{tripId:int}")]
    [Authorize(Policy = AuthorizationPolicies.ReadTrips)]
    [ProducesResponseType<TripReadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripReadDto>> GetTrip(int tripId, CancellationToken cancellationToken)
    {
        var trip = await tripService.GetTripAsync(User, tripId, cancellationToken);
        return trip is null ? NotFound() : Ok(trip);
    }

    /// <summary>Повертає хронологічний маршрут і підтвердження бронювань, якщо користувач має доступ до поїздки.</summary>
    /// <param name="tripId">Ідентифікатор запитаної поїздки.</param>
    /// <param name="query">Параметри сторінки, фільтрації та сортування.</param>
    /// <param name="cancellationToken">Токен для скасування запиту до сервісу.</param>
    /// <response code="200">Повертає маршрут поїздки та підтвердження бронювань.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен не містить дозволу <c>read:itinerary</c>.</response>
    /// <response code="404">Поїздку не знайдено або користувач не має до неї доступу.</response>
    [HttpGet("{tripId:int}/itinerary")]
    [Authorize(Policy = AuthorizationPolicies.ReadItinerary)]
    [ProducesResponseType<PagedResult<ItineraryItemReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу read:itinerary.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound, Description = "Поїздку не знайдено або користувач не має до неї доступу.")]
    public async Task<ActionResult<PagedResult<ItineraryItemReadDto>>> GetItinerary(
        int tripId,
        [FromQuery] ItineraryCollectionQuery query,
        CancellationToken cancellationToken)
    {
        var itinerary = await tripService.GetItineraryAsync(User, tripId, query, cancellationToken);
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
    [ProducesResponseType<TripReadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу create:trips.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType, Description = "Надішліть тіло запиту з Content-Type: application/json.")]
    public async Task<ActionResult<TripReadDto>> CreateTrip(
        [FromBody] CreateTripDto request,
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
        return CreatedAtAction(nameof(GetTrip), new { tripId = trip.TripId }, trip);
    }

    /// <summary>Оновлює поїздку, якщо автентифікований користувач є її власником.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="request">Нові дані поїздки у форматі JSON.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="200">Дані поїздки оновлено.</response>
    /// <response code="400">Дані запиту або категорія не пройшли перевірку.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу update:trips.</response>
    /// <response code="404">Поїздки немає або поточний користувач не є власником.</response>
    [HttpPut("{tripId:int}")]
    [Authorize(Policy = AuthorizationPolicies.UpdateTrips)]
    [Consumes("application/json")]
    [ProducesResponseType<TripReadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripReadDto>> UpdateTrip(
        int tripId,
        [FromBody] UpdateTripDto request,
        CancellationToken cancellationToken)
    {
        var result = await tripService.UpdateTripAsync(User, tripId, request, cancellationToken);
        if (result.Status == TripMutationStatus.PlannerAccountNotFound)
        {
            return Forbid();
        }

        if (result.Status == TripMutationStatus.TripNotFound)
        {
            return NotFound();
        }

        if (result.Status == TripMutationStatus.CategoryNotFound)
        {
            ModelState.AddModelError(nameof(request.TripCategoryId), "Категорію поїздки не знайдено.");
            return ValidationProblem(ModelState);
        }

        return Ok(result.Trip);
    }

    /// <summary>Видаляє поїздку, якщо автентифікований користувач є її власником.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="204">Поїздку видалено.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу delete:trips.</response>
    /// <response code="404">Поїздки немає або поточний користувач не є власником.</response>
    [HttpDelete("{tripId:int}")]
    [Authorize(Policy = AuthorizationPolicies.DeleteTrips)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTrip(int tripId, CancellationToken cancellationToken)
    {
        var deleted = await tripService.DeleteTripAsync(User, tripId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}


