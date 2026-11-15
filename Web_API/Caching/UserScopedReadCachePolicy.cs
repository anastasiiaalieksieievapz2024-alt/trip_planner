using System.Security.Claims;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace Web_API.Caching;

/// <summary>Caches successful authenticated reads separately for each user and query string.</summary>
public sealed class UserScopedReadCachePolicy : IOutputCachePolicy
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;
        var userKey = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;
        var canCache = HttpMethods.IsGet(httpContext.Request.Method)
            && user.Identity?.IsAuthenticated == true
            && !string.IsNullOrWhiteSpace(userKey);

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = canCache;
        context.AllowCacheStorage = canCache;
        context.AllowLocking = true;
        context.ResponseExpirationTimeSpan = CacheDuration;
        context.CacheVaryByRules.QueryKeys = "*";
        context.CacheVaryByRules.VaryByValues["authenticated-user"] = userKey ?? string.Empty;

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        if (response.StatusCode != StatusCodes.Status200OK ||
            !StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }
}
