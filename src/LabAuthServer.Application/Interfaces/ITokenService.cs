using LabAuthServer.Application.DTOs;

namespace LabAuthServer.Application.Interfaces;

public interface ITokenService
{
    Task<TokenResponse> IssueAsync(
        TokenIssuanceRequest request,
        CancellationToken cancellationToken = default);
}
