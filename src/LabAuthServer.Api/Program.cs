using LabAuthServer.Api.Extensions;
using LabAuthServer.Api.Health;
using LabAuthServer.Api.Middleware;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.Auditing;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddActiveDirectoryOptions(builder.Configuration, builder.Environment);
builder.Services.AddTokenConfiguration(builder.Configuration);
builder.Services.AddOptions<AuditOptions>()
    .Bind(builder.Configuration.GetSection(AuditOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString) && options.CommandTimeoutSeconds is > 0 and <= 60)
    .ValidateOnStart();
builder.Services.AddScoped<IAuditEventService, SqlAuditEventService>();
builder.Services.AddScoped<AuthorizationAuditMiddleware>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AuthorizationAuditMiddleware>();
app.UseAuthorization();

app.MapGet("/api/v1/health", () => TypedResults.Ok(new HealthResponse("Healthy")))
    .AllowAnonymous()
    .WithName("GetHealth");

app.MapControllers();

app.Run();

public partial class Program;
