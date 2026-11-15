using Microsoft.AspNetCore.Authorization;

namespace Web_API.Authorization;

/// <summary>Requires a named permission or OAuth scope in the validated access token.</summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;


