using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TestController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult Public() => Ok("Цей ресурс доступний усім.");

    [Authorize]
    [HttpGet("protected")]
    public IActionResult Protected() => Ok(new
    {
        Message = "Ви успішно пройшли автентифікацію!",
        UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
    });
}
