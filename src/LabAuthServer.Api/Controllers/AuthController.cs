using System.Text.Json;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.ActiveDirectory;
using Microsoft.Extensions.Options;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Api.Requests;
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
    private readonly TimeSpan _authenticationTimeout;
    private readonly TimeProvider _clock;
    private readonly ILdapConcurrencyLimiter _ldapConcurrencyLimiter;

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
        ILdapConcurrencyLimiter ldapConcurrencyLimiter,
        ILogger<AuthController>? logger = null,
        IAuditEventService? auditEventService = null,
        IOptions<LdapOptions>? ldapOptions = null,
        TimeProvider? clock = null)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
        _authorizationMappingService = authorizationMappingService ?? throw new ArgumentNullException(nameof(authorizationMappingService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _auditEventService = auditEventService;
        _ldapConcurrencyLimiter = ldapConcurrencyLimiter ?? throw new ArgumentNullException(nameof(ldapConcurrencyLimiter));
        _authenticationTimeout = ldapOptions?.Value.AuthenticationTimeout ?? AuthenticationOperation.DefaultTimeout;
        _clock = clock ?? TimeProvider.System;
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
    [RequestBodyLimit(IngressSizePolicy.MaximumLoginBodyBytes)]
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

        using var operation = new AuthenticationOperation(_authenticationTimeout, cancellationToken, _clock);
        try
        {
            operation.ThrowIfCancellationRequested();
            using var ldapLease = await _ldapConcurrencyLimiter.AcquireAsync(operation);
            if (!ldapLease.IsSuccess)
                return await DirectoryFailureAsync(request.Username, ldapLease.Failure!);
            operation.EnterStage(DirectoryFailureStage.CredentialLoading);
            var result = await _authenticationService.AuthenticateAsync(
                request.Username,
                request.Password,
                operation.Token, operation);
            operation.ThrowIfCancellationRequested();

            if (result.IsAuthenticated)
            {
                try
                {
                    operation.EnterStage(DirectoryFailureStage.GroupSearch);
                    var groupResult = await _ldapService.GetUserGroupsAsync(
                        request.Username,
                        request.Password,
                        operation.Token, operation);
                    operation.ThrowIfCancellationRequested();

                    if (!groupResult.IsSuccess)
                    {
                        ldapLease.Dispose();
                        return await DirectoryFailureAsync(request.Username, groupResult.Failure!);
                    }

                    // LDAP and connection cleanup have completed. Unrelated work needs no permit.
                    ldapLease.Dispose();
                    operation.EnterStage(DirectoryFailureStage.RoleMapping);
                    var authorizationResult = await _authorizationMappingService.MapGroupsToRolesAsync(
                        groupResult.Groups,
                        operation.Token);
                    operation.ThrowIfCancellationRequested();

                    operation.EnterStage(DirectoryFailureStage.TokenIssuance);
                    var tokenResponse = await _tokenService.IssueAsync(
                        new TokenIssuanceRequest
                        {
                            Subject = request.Username,
                            Roles = authorizationResult.Roles.Take(1).ToArray(),
                            Scopes = Array.Empty<string>()
                        },
                        operation.Token);
                    operation.Complete();

                    await RecordAuditAsync(
                        AuditEventTypes.LoginSuccess,
                        request.Username,
                        StatusCodes.Status200OK,
                        true,
                        role: authorizationResult.Roles.Take(1).SingleOrDefault());

                    return Ok(tokenResponse);
                }
                catch (Exception) when (operation.Failure is not null)
                {
                    ldapLease.Dispose();
                    return await DirectoryFailureAsync(request.Username, operation.Failure!);
                }
                catch (Exception)
                {
                    ldapLease.Dispose();
                    _logger.LogError(
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

            ldapLease.Dispose();
            return await DirectoryFailureAsync(request.Username, result.Failure!);
        }
        catch (Exception) when (operation.Failure is not null)
        {
            return await DirectoryFailureAsync(request.Username, operation.Failure!);
        }
    }

    private async Task<IActionResult> DirectoryFailureAsync(string username, DirectoryFailure failure)
    {
        var statusCode = failure.Category switch
        {
            AuthenticationFailureCategory.InvalidCredentials => StatusCodes.Status401Unauthorized,
            AuthenticationFailureCategory.InvalidRequest => StatusCodes.Status400BadRequest,
            AuthenticationFailureCategory.DirectoryUnavailable or AuthenticationFailureCategory.ProtocolFailure => StatusCodes.Status503ServiceUnavailable,
            AuthenticationFailureCategory.ResourceExhausted => StatusCodes.Status503ServiceUnavailable,
            AuthenticationFailureCategory.Timeout => StatusCodes.Status504GatewayTimeout,
            AuthenticationFailureCategory.Cancelled => StatusCodes.Status499ClientClosedRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        var correlation = HttpContext.Items[CorrelationContext.ItemKey] as CorrelationContext;
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.3.2",
            Status = statusCode,
            Detail = failure.SafeMessage
        };
        if (correlation is not null) problem.Extensions["correlationId"] = correlation.CorrelationId.ToString("D");
        var eventType = failure.Category is AuthenticationFailureCategory.InvalidCredentials
            or AuthenticationFailureCategory.InvalidRequest or AuthenticationFailureCategory.Cancelled
            ? AuditEventTypes.LoginFailure : AuditEventTypes.LdapFailure;
        _logger.Log(failure.Category is AuthenticationFailureCategory.InvalidCredentials or AuthenticationFailureCategory.InvalidRequest
                ? LogLevel.Information : failure.Category == AuthenticationFailureCategory.Cancelled ? LogLevel.Warning : LogLevel.Error,
            "Login failed: {Category} at {Stage}, reason {Reason}, diagnostic {DiagnosticSource}:{DiagnosticCode}, correlation {CorrelationId}.",
            failure.Category, failure.Stage, failure.Reason, failure.DiagnosticSource, failure.DiagnosticCode, correlation?.CorrelationId);
        await RecordAuditAsync(eventType, username, statusCode, false, failure: failure);
        return StatusCode(statusCode, problem);
    }
    private async Task RecordAuditAsync(
        string eventTypeCode,
        string? username,
        int statusCode,
        bool success,
        string? detail = null,
        string? role = null,
        DirectoryFailure? failure = null)
    {
        if (_auditEventService is null)
            return;

        try
        {
            var correlation = HttpContext.Items[Requests.CorrelationContext.ItemKey] as Requests.CorrelationContext;
            // Audit storage has a smaller identity field than the approved login/JWT contract.
            // Preserve the event without truncating an identity into a different apparent user.
            var identityOmitted = username is { Length: > 256 };
            var auditUsername = identityOmitted ? null : username;
            await _auditEventService.WriteAsync(new AuditEvent
            {
                EventTypeCode = eventTypeCode,
                CorrelationId = correlation?.CorrelationId ?? Guid.NewGuid(),
                RequestId = correlation?.RequestId ?? HttpContext.TraceIdentifier,
                Username = auditUsername,
                Subject = auditUsername,
                Role = role,
                Endpoint = "/api/v1/auth/login",
                HttpMethod = Request.Method,
                StatusCode = (short)statusCode,
                Success = success,
                ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ServerName = Environment.MachineName,
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
                DetailsJson = failure is not null ? JsonSerializer.Serialize(new
                {
                    failureCategory = failure.Category.ToString(), stage = failure.Stage.ToString(),
                    reason = failure.Reason.ToString(), diagnosticSource = failure.DiagnosticSource.ToString(),
                    diagnosticCode = failure.DiagnosticCode, identityOmitted
                }) : detail is null && !identityOmitted ? null : JsonSerializer.Serialize(new { failureCategory = detail, identityOmitted })
            }, HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _logger.LogWarning("Audit event could not be persisted for {EventTypeCode}.", eventTypeCode);
        }
    }

}
