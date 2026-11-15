using System.IO.Compression;
using Web_API.Caching;
using Kpz.Lab2.Data;
using Web_API.Authorization;
using Web_API.Services;
using Web_API.ErrorHandling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.AddOutputCache(options =>
    options.AddPolicy("UserScopedRead", new UserScopedReadCachePolicy()));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerOperationSecurityTransformer>();
});

var auth0Domain = builder.Configuration["Auth0:Domain"]
    ?? throw new InvalidOperationException("Auth0:Domain must be configured.");
var audience = builder.Configuration["Auth0:Audience"]
    ?? throw new InvalidOperationException("Auth0:Audience must be configured.");
var connectionString = builder.Configuration.GetConnectionString("KpzDb")
    ?? throw new InvalidOperationException("Connection string KpzDb must be configured.");

// DbContext і сервіс поїздок мають один scope запиту; сервіс використовує цей контекст.
builder.Services.AddDbContext<KpzDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IItineraryItemService, ItineraryItemService>();
builder.Services.AddScoped<ITripMemberService, TripMemberService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain.Trim().TrimEnd('/')}/";
        options.Audience = audience;
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ReadTrips, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.ReadTripsPermission));
    });
    options.AddPolicy(AuthorizationPolicies.CreateTrips, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.CreateTripsPermission));
    });
    options.AddPolicy(AuthorizationPolicies.UpdateTrips, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.UpdateTripsPermission));
    });
    options.AddPolicy(AuthorizationPolicies.DeleteTrips, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.DeleteTripsPermission));
    });
    options.AddPolicy(AuthorizationPolicies.ReadItinerary, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.ReadItineraryPermission));
    });
    options.AddPolicy(AuthorizationPolicies.CreateItinerary, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.CreateItineraryPermission));
    });
    options.AddPolicy(AuthorizationPolicies.UpdateItinerary, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.UpdateItineraryPermission));
    });
    options.AddPolicy(AuthorizationPolicies.DeleteItinerary, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.DeleteItineraryPermission));
    });
    options.AddPolicy(AuthorizationPolicies.ReadMembers, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.ReadMembersPermission));
    });
    options.AddPolicy(AuthorizationPolicies.CreateMembers, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.CreateMembersPermission));
    });
    options.AddPolicy(AuthorizationPolicies.DeleteMembers, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(AuthorizationPolicies.DeleteMembersPermission));
    });

    // Everything is private by default. Public endpoints must opt in with [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
// Перевіряє лише claims і не зберігає стан чи scoped-залежності, тому може бути singleton.
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
// Читає власника/учасника з бази через scoped DbContext, тому має lifetime scoped.
builder.Services.AddScoped<IAuthorizationHandler, ReadTripHandler>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KpzDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options
        .WithTitle("Trip Planner API")
        .AddPreferredSecuritySchemes("Bearer"))
        .AllowAnonymous();
}
//Перехоплення необроблених винятків та повернення безпечної відповіді клієнту
app.UseExceptionHandler();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    var (error, message) = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => ("Unauthorized", "Потрібна автентифікація. Передайте дійсний access token."),
        StatusCodes.Status403Forbidden => ("Forbidden", "Недостатньо дозволів для виконання цього запиту."),
        StatusCodes.Status404NotFound => ("Not Found", "Ресурс не знайдено або у вас немає до нього доступу."),
        _ => ("Error", "Запит не вдалося виконати.")
    };

    response.ContentType = "application/json; charset=utf-8";
    await response.WriteAsJsonAsync(new
    {
        Status = response.StatusCode,
        Error = error,
        Message = message,
        Path = statusCodeContext.HttpContext.Request.Path.Value
    });
});
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.MapControllers();

app.Run();

public partial class Program;


