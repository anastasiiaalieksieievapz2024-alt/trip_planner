using Web_API.Authorization;
using Web_API.Contracts;
using Web_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Web_API.Controllers;

/// <summary>Кінцеві точки читання й керування учасниками поїздки.</summary>
[ApiController]
[Route("api/trips/{tripId:int}/members")]
[Produces("application/json")]
public sealed class TripMembersController(ITripMemberService memberService, IOutputCacheStore outputCacheStore) : ControllerBase
{
    private const string ReadCacheTag = "trip-api";

    /// <summary>Повертає учасників поїздки для її власника або учасника з правом читання.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="query">Параметри сторінки, фільтрації та сортування.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="200">Список учасників поїздки.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу read:members.</response>
    /// <response code="404">Поїздки немає або користувач не має до неї доступу.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadMembers)]
    [OutputCache(PolicyName = "UserScopedRead", Tags = new[] { ReadCacheTag })]
    [ProducesResponseType<PagedResult<TripMemberReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TripMemberReadDto>>> GetMembers(
        int tripId, [FromQuery] TripMemberCollectionQuery query, CancellationToken cancellationToken)
    {
        var members = await memberService.GetMembersAsync(User, tripId, query, cancellationToken);
        return members is null ? NotFound() : Ok(members);
    }

    /// <summary>Додає зареєстрованого користувача до поїздки як читача. Виконати може лише власник.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="request">Email зареєстрованого користувача.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="201">Користувача додано як учасника TripReader.</response>
    /// <response code="400">Email має некоректний формат.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу create:members.</response>
    /// <response code="404">Поїздки немає, поточний користувач не є власником або email не зареєстровано в планері.</response>
    /// <response code="409">Користувач уже є учасником поїздки.</response>
    /// <response code="415">Потрібно передати JSON.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CreateMembers)]
    [Consumes("application/json")]
    [ProducesResponseType<TripMemberReadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<TripMemberReadDto>> AddMember(
        int tripId, [FromBody] AddTripMemberDto request, CancellationToken cancellationToken)
    {
        var result = await memberService.AddMemberAsync(User, tripId, request.Email, cancellationToken);
        if (result.Status is TripMemberMutationStatus.TripNotFound or TripMemberMutationStatus.UserNotFound)
        {
            return NotFound();
        }

        if (result.Status == TripMemberMutationStatus.AlreadyMember)
        {
            return Conflict(new ApiErrorResponse(
                StatusCodes.Status409Conflict,
                "Conflict",
                "Користувач уже є учасником цієї поїздки.",
                Request.Path));
        }

        var member = result.Member!;
        await outputCacheStore.EvictByTagAsync(ReadCacheTag, cancellationToken);
        return CreatedAtAction(nameof(GetMembers), new { tripId }, member);
    }

    /// <summary>Видаляє учасника з поїздки. Виконати може лише її власник.</summary>
    /// <param name="tripId">Ідентифікатор поїздки.</param>
    /// <param name="userId">Ідентифікатор учасника.</param>
    /// <param name="cancellationToken">Токен скасування запиту.</param>
    /// <response code="204">Учасника видалено.</response>
    /// <response code="401">Токен відсутній або недійсний.</response>
    /// <response code="403">Немає дозволу delete:members.</response>
    /// <response code="404">Поїздки або учасника немає чи поточний користувач не є власником.</response>
    [HttpDelete("{userId:int}")]
    [Authorize(Policy = AuthorizationPolicies.DeleteMembers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMember(
        int tripId, int userId, CancellationToken cancellationToken)
    {
        var deleted = await memberService.DeleteMemberAsync(User, tripId, userId, cancellationToken);
        if (deleted)
        {
            await outputCacheStore.EvictByTagAsync(ReadCacheTag, cancellationToken);
        }
        return deleted ? NoContent() : NotFound();
    }
}


