using LabAuthServer.Application.Services;
using LabAuthServer.Application.DTOs;

namespace LabAuthServer.Application.Interfaces;

/// <summary>Application-instance admission for the login's complete LDAP phase.</summary>
public interface ILdapConcurrencyLimiter
{
    /// <summary>The caller must retain the lease until all protected work and cleanup have completed.</summary>
    Task<LdapAdmissionResult> AcquireAsync(AuthenticationOperation operation);
}
