using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using Microsoft.Extensions.Logging;

namespace LabAuthServer.Infrastructure.Services;

internal static class LdapFailureLogging
{
    public static void Write(ILogger logger, DirectoryFailure failure)
    {
        var level = failure.Category is AuthenticationFailureCategory.InvalidCredentials or AuthenticationFailureCategory.InvalidRequest
            ? LogLevel.Information : failure.Category == AuthenticationFailureCategory.Cancelled ? LogLevel.Warning : LogLevel.Error;
        logger.Log(level, "Directory operation failed: {Category} at {Stage}, reason {Reason}, diagnostic {DiagnosticSource}:{DiagnosticCode}.",
            failure.Category, failure.Stage, failure.Reason, failure.DiagnosticSource, failure.DiagnosticCode);
    }
}
