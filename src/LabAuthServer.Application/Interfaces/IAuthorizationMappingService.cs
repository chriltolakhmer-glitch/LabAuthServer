namespace LabAuthServer.Application.Interfaces;

public interface IAuthorizationMappingService
{
    Task<AuthorizationMappingResult> MapGroupsToRolesAsync(
        IReadOnlyCollection<string> groupIdentifiers,
        CancellationToken cancellationToken = default);
}

public sealed record AuthorizationMappingResult
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
