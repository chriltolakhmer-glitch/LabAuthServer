using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabAuthServer.Api.Controllers;

/// <summary>
/// Authentication endpoints for user login.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILdapService _ldapService;
    private readonly IAuthorizationMappingService _authorizationMappingService;
    private readonly ITokenService _tokenService;
    private readonly IAuditEventService? _auditEventService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authenticationService">Authentication service.</param>
    /// <param name="authorizationMappingService">Authorization mapping service.</param>
    /// <param name="tokenService">Token issuance service.</param>
    public AuthController(
        IAuthenticationService authenticationService,
        ILdapService ldapService,
        IAuthorizationMappingService authorizationMappingService,
        ITokenService tokenService,
        ILogger<AuthController>? logger = null,
        IAuditEventService? auditEventService = null)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
        _authorizationMappingService = authorizationMappingService ?? throw new ArgumentNullException(nameof(authorizationMappingService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _auditEventService = auditEventService;
        _logger = logger ?? NullLogger<AuthController>.Instance;
    }

    /// <summary>
    /// Authenticates a user with the provided credentials.
    /// </summary>
    /// <param name="request">Login request with username and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with authenticated user information.
    /// 401, 503, 504, or 500 according to the authentication failure category.
    /// 400 Bad Request if request is invalid.
    /// </returns>
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.IsHttps)
        {
            await RecordAuditAsync(AuditEventTypes.ValidationError, null, StatusCodes.Status400BadRequest, false, "https-required");
            return BadRequest(new ProblemDetails
            {
                Title = "HTTPS is required.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Authentication requests must use HTTPS."
            });
        }

        if (request == null)
        {
            await RecordAuditAsync(AuditEventTypes.ValidationError, null, StatusCodes.Status400BadRequest, false, "request-body-required");
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });
        }

        var result = await _authenticationService.AuthenticateAsync(
            request.Username,
            request.Password,
            cancellationToken);

        if (result.IsAuthenticated)
        {
            try
            {
                var groupIdentifiers = await _ldapService.GetUserGroupsAsync(
                    request.Username,
                    request.Password,
                    cancellationToken);

                var authorizationResult = await _authorizationMappingService.MapGroupsToRolesAsync(
                    groupIdentifiers,
                    cancellationToken);

                var tokenResponse = await _tokenService.IssueAsync(
                    new TokenIssuanceRequest
                    {
                        Subject = request.Username,
                        Roles = authorizationResult.Roles.Take(1).ToArray(),
                        Scopes = Array.Empty<string>()
                    },
                    cancellationToken);

                await RecordAuditAsync(
                    AuditEventTypes.LoginSuccess,
                    request.Username,
                    StatusCodes.Status200OK,
                    true,
                    role: authorizationResult.Roles.Take(1).SingleOrDefault());

                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Authorization or token issuance failed for {Username} during {FailureStage}.",
                    request.Username,
                    "group-mapping-or-token-issuance");
                await RecordAuditAsync(AuditEventTypes.UnhandledException, request.Username, StatusCodes.Status500InternalServerError, false, "login-post-authentication");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                        Title = "Token issuance failed.",
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = "Authentication succeeded but authorization or token issuance failed."
                    });
            }
        }

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.3.2",
            Detail = result.ErrorMessage ?? "Authentication failed."
        };

        var statusCode = result.FailureCategory switch
        {
            AuthenticationFailureCategory.InvalidCredentials => StatusCodes.Status401Unauthorized,
            AuthenticationFailureCategory.InvalidRequest => StatusCodes.Status400BadRequest,
            AuthenticationFailureCategory.DirectoryUnavailable => StatusCodes.Status503ServiceUnavailable,
            AuthenticationFailureCategory.Timeout => StatusCodes.Status504GatewayTimeout,
            AuthenticationFailureCategory.Cancelled => StatusCodes.Status499ClientClosedRequest,
            AuthenticationFailureCategory.Configuration => StatusCodes.Status500InternalServerError,
            AuthenticationFailureCategory.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        problemDetails.Status = statusCode;
        var eventType = result.FailureCategory is AuthenticationFailureCategory.DirectoryUnavailable
            or AuthenticationFailureCategory.Timeout
            or AuthenticationFailureCategory.Configuration
            or AuthenticationFailureCategory.Unexpected
            ? AuditEventTypes.LdapFailure
            : AuditEventTypes.LoginFailure;
        await RecordAuditAsync(eventType, request.Username, statusCode, false, result.FailureCategory.ToString());
        return StatusCode(statusCode, problemDetails);
    }

    private async Task RecordAuditAsync(
        string eventTypeCode,
        string? username,
        int statusCode,
        bool success,
        string? detail = null,
        string? role = null)
    {
        if (_auditEventService is null)
            return;

        try
        {
            var correlation = HttpContext.Items[Requests.CorrelationContext.ItemKey] as Requests.CorrelationContext;
            await _auditEventService.WriteAsync(new AuditEvent
            {
                EventTypeCode = eventTypeCode,
                CorrelationId = correlation?.CorrelationId ?? Guid.NewGuid(),
                RequestId = correlation?.RequestId ?? HttpContext.TraceIdentifier,
                Username = username,
                Subject = username,
                Role = role,
                Endpoint = "/api/v1/auth/login",
                HttpMethod = Request.Method,
                StatusCode = (short)statusCode,
                Success = success,
                ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ServerName = Environment.MachineName,
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
                DetailsJson = detail is null ? null : $"{{\"failureCategory\":\"{detail}\"}}"
            }, HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _logger.LogWarning("Audit event could not be persisted for {EventTypeCode}.", eventTypeCode);
        }
    }

}
