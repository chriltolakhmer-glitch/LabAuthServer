namespace LabAuthServer.Application.Interfaces;

public interface ILdapServiceAccountCredentialProvider
{
    Task<string> GetPasswordAsync(CancellationToken cancellationToken = default);
}
