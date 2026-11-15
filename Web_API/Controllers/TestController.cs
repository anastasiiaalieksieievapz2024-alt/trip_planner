using System.Security.Claims;
using Web_API.Authorization;
using Web_API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web_API.Controllers;

/// <summary>Кінцеві точки для перевірки доступності API та дозволів access token.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TestController : ControllerBase
{
    /// <summary>Перевіряє доступність API без автентифікації.</summary>
    /// <response code="200">API доступний; повертається повідомлення.</response>
    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType<string>(StatusCodes.Status200OK, Description = "Повідомлення про доступність API.")]
    public IActionResult Public() => Ok("Цей ресурс доступний усім.");

    /// <summary>Повертає ідентифікатор автентифікованого користувача Auth0.</summary>
    /// <response code="200">Токен дійсний; повертається ідентифікатор користувача.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    [Authorize]
    [HttpGet("protected")]
    [ProducesResponseType<AuthenticatedUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    public ActionResult<AuthenticatedUserResponse> Protected() => Ok(new AuthenticatedUserResponse(
        "Ви успішно пройшли автентифікацію!",
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
    ));

    /// <summary>Перевіряє наявність дозволу <c>read:trips</c> в access token.</summary>
    /// <response code="200">Токен містить потрібний дозвіл.</response>
    /// <response code="401">Токен відсутній, недійсний або прострочений.</response>
    /// <response code="403">Токен дійсний, але не містить дозволу <c>read:trips</c>.</response>
    [Authorize(Policy = AuthorizationPolicies.ReadTrips)]
    [HttpGet("planner")]
    [ProducesResponseType<PlannerPermissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized, Description = "Потрібна автентифікація.")]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden, Description = "У токені немає дозволу read:trips.")]
    public ActionResult<PlannerPermissionResponse> RegisteredPlanner() => Ok(new PlannerPermissionResponse(
        "Токен має дозвіл read:trips.",
        User.FindFirst("sub")?.Value
    ));
}


