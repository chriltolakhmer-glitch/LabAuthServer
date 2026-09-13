using System.Net;
using System.Text.Json;
using LabAuthServer.Application.Constants;

namespace LabAuthServer.Application.Auditing;

public static class AuditEventValidator
{
    public const int MaximumIdentityLength = 256;

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "accessToken", "refreshToken", "authorization", "headers",
        "requestBody", "secret", "privateKey", "certificate", "dpapi", "stackTrace",
        "exception", "ldapResponse", "ldapFilter", "connectionString"
    };

    public static IReadOnlyList<string> Validate(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var failures = new List<string>();

        if (!AuditEventTypes.All.Contains(auditEvent.EventTypeCode)) failures.Add("Event type is not approved.");
        if (auditEvent.CorrelationId == Guid.Empty) failures.Add("Correlation ID is required.");
        CheckLength(auditEvent.RequestId, 128, nameof(auditEvent.RequestId), failures);
        CheckLength(auditEvent.Username, MaximumIdentityLength, nameof(auditEvent.Username), failures);
        CheckLength(auditEvent.Subject, MaximumIdentityLength, nameof(auditEvent.Subject), failures);
        CheckLength(auditEvent.Role, 64, nameof(auditEvent.Role), failures);
        CheckLength(auditEvent.Endpoint, 256, nameof(auditEvent.Endpoint), failures);
        CheckLength(auditEvent.HttpMethod, 16, nameof(auditEvent.HttpMethod), failures);
        CheckLength(auditEvent.ClientIp, 45, nameof(auditEvent.ClientIp), failures);
        CheckLength(auditEvent.ServerName, 128, nameof(auditEvent.ServerName), failures);
        CheckLength(auditEvent.ApplicationVersion, 64, nameof(auditEvent.ApplicationVersion), failures);

        if (auditEvent.Role is not null && !AuthorizationRoles.All.Contains(auditEvent.Role, StringComparer.Ordinal)) failures.Add("Role is not approved.");
        if (auditEvent.StatusCode is < 100 or > 599) failures.Add("Status code is invalid.");
        if (auditEvent.HttpMethod is not null && !new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE" }.Contains(auditEvent.HttpMethod, StringComparer.Ordinal)) failures.Add("HTTP method is not approved.");
        if (auditEvent.ClientIp is not null && !IPAddress.TryParse(auditEvent.ClientIp, out _)) failures.Add("Client IP is invalid.");

        if (auditEvent.DetailsJson is not null)
        {
            if (auditEvent.DetailsJson.Length > 16384) failures.Add("Details JSON is too large.");
            try
            {
                using var document = JsonDocument.Parse(auditEvent.DetailsJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object) failures.Add("Details JSON must be an object.");
                if (document.RootElement.EnumerateObject().Any(property => ContainsSensitiveProperty(property))) failures.Add("Details JSON contains a prohibited property.");
            }
            catch (JsonException)
            {
                failures.Add("Details JSON is invalid.");
            }
        }

        return failures;
    }

    private static bool ContainsSensitiveProperty(JsonProperty property)
    {
        if (SensitivePropertyNames.Contains(property.Name)) return true;
        return ContainsSensitiveValue(property.Value);
    }

    private static bool ContainsSensitiveValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject().Any(ContainsSensitiveProperty),
            JsonValueKind.Array => value.EnumerateArray().Any(ContainsSensitiveValue),
            _ => false
        };
    }

    private static void CheckLength(string? value, int maximum, string name, ICollection<string> failures)
    {
        if (value is not null && value.Length > maximum) failures.Add($"{name} is too long.");
    }
}
