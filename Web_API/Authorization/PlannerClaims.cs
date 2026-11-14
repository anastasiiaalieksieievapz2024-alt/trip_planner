using System.Security.Claims;

namespace Web_API.Authorization;

public static class PlannerClaims
{
    public const string NamespacedEmail = "https://trip-planner-api/email";

    public static string? GetEmail(ClaimsPrincipal user)
    {
        var email = user.FindFirst("email")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst(NamespacedEmail)?.Value;

        return string.IsNullOrWhiteSpace(email) ? null : email;
    }
}
