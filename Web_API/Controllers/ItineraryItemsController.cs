using Web_API.Authorization;
using Web_API.Contracts;
using Web_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web_API.Controllers;

/// <summary>CRUD кінцеві точки пунктів маршруту поїздки.</summary>
[ApiController]
[Route("api/trips/{tripId:int}/items")]
[Produces("application/json")]
public sealed class ItineraryItemsController(IItineraryItemService itemService) : ControllerBase
{
    /// <summary>Повертає пункти маршруту для власника або учасника поїздки.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="200">Список пунктів маршруту.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу read:itinerary.</response>
    /// <response code="404">Поїздки немає або користувач не є власником чи учасником.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadItinerary)]
    [ProducesResponseType<IReadOnlyList<ItineraryItemReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ItineraryItemReadDto>>> GetItems(int tripId, CancellationToken cancellationToken)
    {
        var items = await itemService.GetItemsAsync(User, tripId, cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }

    /// <summary>Повертає один пункт маршруту поїздки.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="itemId">Ідентифікатор пункту маршруту.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="200">Пункт маршруту знайдено.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу read:itinerary.</response>
    /// <response code="404">Поїздки або пункту немає чи користувач не має доступу.</response>
    [HttpGet("{itemId:int}")]
    [Authorize(Policy = AuthorizationPolicies.ReadItinerary)]
    [ProducesResponseType<ItineraryItemReadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItineraryItemReadDto>> GetItem(int tripId, int itemId, CancellationToken cancellationToken)
    {
        var item = await itemService.GetItemAsync(User, tripId, itemId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Додає пункт до маршруту поїздки, якою володіє користувач.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="request">Дані пункту маршруту.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="201">Пункт маршруту створено.</response>
    /// <response code="400">Дані пункту некоректні або вказаний тип не існує.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу create:itinerary.</response>
    /// <response code="404">Поїздки немає або користувач не є її власником.</response>
    /// <response code="415">Потрібно передати JSON.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CreateItinerary)]
    [Consumes("application/json")]
    [ProducesResponseType<ItineraryItemReadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<ItineraryItemReadDto>> CreateItem(
        int tripId, [FromBody] CreateItineraryItemDto request, CancellationToken cancellationToken)
    {
        var result = await itemService.CreateAsync(User, tripId, request, cancellationToken);
        if (result.Status == ItineraryItemMutationStatus.TripNotFound)
        {
            return NotFound();
        }

        if (result.Status == ItineraryItemMutationStatus.TypeNotFound)
        {
            ModelState.AddModelError(nameof(request.ItineraryItemTypeId), "Тип пункту маршруту не знайдено.");
            return ValidationProblem(ModelState);
        }

        var item = result.Item!;
        return CreatedAtAction(nameof(GetItem), new { tripId, itemId = item.ItemId }, item);
    }

    /// <summary>Повністю оновлює пункт маршруту; змінювати його може лише власник поїздки.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="itemId">Ідентифікатор пункту маршруту.</param>
    /// <param name="request">Нові дані пункту маршруту.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="200">Пункт оновлено.</response>
    /// <response code="400">Дані пункту некоректні або вказаний тип не існує.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу update:itinerary.</response>
    /// <response code="404">Поїздки чи пункту немає або користувач не є власником.</response>
    /// <response code="415">Потрібно передати JSON.</response>
    [HttpPut("{itemId:int}")]
    [Authorize(Policy = AuthorizationPolicies.UpdateItinerary)]
    [Consumes("application/json")]
    [ProducesResponseType<ItineraryItemReadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<ItineraryItemReadDto>> UpdateItem(
        int tripId, int itemId, [FromBody] UpdateItineraryItemDto request, CancellationToken cancellationToken)
    {
        var result = await itemService.UpdateAsync(User, tripId, itemId, request, cancellationToken);
        if (result.Status is ItineraryItemMutationStatus.TripNotFound or ItineraryItemMutationStatus.ItemNotFound)
        {
            return NotFound();
        }

        if (result.Status == ItineraryItemMutationStatus.TypeNotFound)
        {
            ModelState.AddModelError(nameof(request.ItineraryItemTypeId), "Тип пункту маршруту не знайдено.");
            return ValidationProblem(ModelState);
        }

        return Ok(result.Item);
    }

    /// <summary>Видаляє пункт маршруту; видаляти його може лише власник поїздки.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="itemId">Ідентифікатор пункту маршруту.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="204">Пункт видалено.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу delete:itinerary.</response>
    /// <response code="404">Поїздки чи пункту немає або користувач не є власником.</response>
    [HttpDelete("{itemId:int}")]
    [Authorize(Policy = AuthorizationPolicies.DeleteItinerary)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(int tripId, int itemId, CancellationToken cancellationToken)
    {
        var deleted = await itemService.DeleteAsync(User, tripId, itemId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}


