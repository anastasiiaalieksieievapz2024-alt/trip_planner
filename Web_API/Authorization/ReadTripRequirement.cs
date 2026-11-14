using Microsoft.AspNetCore.Authorization;

namespace Web_API.Authorization;

/// <summary>Resource requirement for reading one specific trip.</summary>
public sealed class ReadTripRequirement : IAuthorizationRequirement;
