using Kpz.Lab2.Data;
using Web_API.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

builder.Services.AddDbContext<KpzDbContext>(options => options.UseSqlServer(connectionString));

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

    // Everything is private by default. Public endpoints must opt in with [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
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
app.MapControllers();

app.Run();

public partial class Program;
