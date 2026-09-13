using LabAuthServer.Api.Extensions;
using LabAuthServer.Api.Health;
using LabAuthServer.Api.Middleware;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.Auditing;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddHsts(options =>
{
    // Approved Phase 3 configuration: one year, no includeSubDomains, no preload.
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = false;
    options.Preload = false;
});
builder.Services.Configure<Microsoft.AspNetCore.HostFiltering.HostFilteringOptions>(options =>
    options.AllowEmptyHosts = false);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Login", _ => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: "login",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddActiveDirectoryOptions(builder.Configuration, builder.Environment);
builder.Services.AddTokenConfiguration(builder.Configuration);
builder.Services.AddLicenseConfiguration(builder.Configuration);
builder.Services.AddOptions<AuditOptions>()
    .Bind(builder.Configuration.GetSection(AuditOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString) && options.CommandTimeoutSeconds is > 0 and <= 60)
    .ValidateOnStart();
builder.Services.AddScoped<IAuditEventService, SqlAuditEventService>();
builder.Services.AddScoped<AuthorizationAuditMiddleware>();

var app = builder.Build();

// Load and validate the license once at startup. A missing, unreadable or invalid license
// yields restricted Community mode; it never crashes startup and never weakens security.
try
{
    _ = app.Services.GetRequiredService<LabAuthServer.Application.Licensing.ILicensePolicyProvider>();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "License subsystem could not be initialized; entering restricted Community mode.");
}

app.UseMiddleware<ApiResponseHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();
app.UseHsts();
app.UseHttpsRedirection();
app.UseRouting();
app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next().ConfigureAwait(false);
});
app.UseMiddleware<GeneralHeaderSizeMiddleware>();
app.UseMiddleware<RequestBodySizeMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<AuthorizationSizeMiddleware>();
app.UseAuthentication();
app.UseMiddleware<AuthorizationAuditMiddleware>();
app.UseAuthorization();

app.MapGet("/api/v1/health", () => TypedResults.Ok(new HealthResponse("Healthy")))
    .AllowAnonymous()
    .WithName("GetHealth");

app.MapControllers();

app.Run();

public partial class Program;
