# Authentication Design — Phase 3

**Status:** Approved and implemented  
**Date:** 2026-08-30  
**Foundation:** LDAPS RootDSE connectivity (approved and implemented)

---

## Executive Summary

This document defines the approved authentication architecture for LabAuthServer, building on the approved LDAPS RootDSE connectivity phase. It defines how users are authenticated against Active Directory using LDAPS and how credentials are handled securely.

---

## 1. Authentication Scope & Constraints

### Current Foundation

- ✅ LDAPS RootDSE connectivity verified (DC01.lab.local:636)
- ✅ LdapOptions configuration in place
- ✅ ILdapService infrastructure abstraction established
- ✅ Certificate validation: platform default (Windows certificate store, no bypass)
- ✅ Authentication requires LDAPS over TCP 636

### Constraints from Project Governance

From `Coding_Standard_and_SOP.md`:
- "Never store plaintext passwords, access tokens, refresh tokens, keys, certificates, or secrets in source code."
- "Use LDAPS with production certificate validation for Active Directory authentication."
- "Do not disable TLS certificate validation as a permanent solution."
- "Use `ILogger<T>` with structured logging. Do not log passwords, tokens, raw authorization headers, or sensitive directory responses."

From `AGENTS.md`:
- "Do not introduce NuGet packages or other dependencies without approval."
- "Do not change the authentication architecture without approval."

### What This Phase Will NOT Include

- ❌ Authorization (access control, role-based access, scope validation)
- ❌ Password hashing/storage (authentication only, not persistence)
- ❌ Multi-factor authentication (MFA)
- ❌ Single sign-on (SSO) or federation
- ❌ Session management or token persistence
- ❌ Account lockout or brute-force protection

---

## 2. Proposed Authentication Architecture

### 2.1 High-Level Flow

```
User → [Credentials] → API /api/v1/auth/login (POST)
                         ↓
                    Application Layer
                    (IAuthenticationService)
                         ↓
                    Infrastructure Layer
                    (LdapAuthenticationService)
                         ↓
                    LDAP over LDAPS
                    (Bind with user credentials)
                         ↓
                    Success: Return authentication result
                    Failure: Return error response
```

### 2.2 Layer Responsibilities

| Layer | Component | Responsibility |
|-------|-----------|-----------------|
| **Domain** | (No additions proposed) | Remain independent |
| **Application** | `IAuthenticationService` interface | Define authentication contract; abstraction |
| **Application** | `LoginRequest` DTO | Input validation and binding |
| **Application** | `AuthenticationResult` DTO | Return successful/failed authentication state |
| **Infrastructure** | `LdapAuthenticationService` | LDAP bind operation; error handling |
| **Api** | `AuthController` | HTTP /api/v1/auth/login endpoint; status codes |

### 2.3 Data Flow (Detailed)

```
1. Client POST /api/v1/auth/login
   Request: { "username": "user@lab.local", "password": "..." }
   
2. Api.Controllers.AuthController.Login()
   - Validate request (built-in model validation)
   - Inject IAuthenticationService
   - Call AuthenticationService.AuthenticateAsync(username, password)
   
3. Application.Services.AuthenticationService.AuthenticateAsync()
   - Validate inputs (username not empty, password not empty)
   - Call ILdapService for LDAP operations
   - Return AuthenticationResult (success or failure)
   
4. Infrastructure.Services.LdapAuthenticationService (via ILdapService extension)
   - Create LDAP connection to DC01.lab.local:636
   - Attempt bind with provided credentials
   - Return success/failure to Application layer
   - Log result (no passwords in logs)
   
5. AuthController returns HTTP response
   Success (200 OK): { "authenticated": true, ... }
   Failure (401 Unauthorized): ProblemDetails error response
```

---

## 3. Authentication Components (Proposed)

### 3.1 Application Layer: IAuthenticationService Interface

**Location:** `src/LabAuthServer.Application/Interfaces/IAuthenticationService.cs`

**Purpose:** Abstraction for authentication operations (no LDAP details)

**Proposed members:**

```csharp
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user against the directory using the provided credentials.
    /// </summary>
    /// <param name="username">Username or distinguished name</param>
    /// <param name="password">Password (not stored; used for bind only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AuthenticationResult with success status and error message (if failed)</returns>
    Task<AuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public sealed record AuthenticationResult
{
    /// <summary>
    /// Gets a value indicating whether authentication succeeded.
    /// </summary>
    public required bool IsAuthenticated { get; init; }

    /// <summary>
    /// Gets the error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
```

### 3.2 Application Layer: LoginRequest DTO

**Location:** `src/LabAuthServer.Application/DTOs/LoginRequest.cs`

**Purpose:** Validate and bind incoming login POST request

**Proposed members:**

```csharp
public sealed record LoginRequest
{
    /// <summary>
    /// Username or email (e.g., "user@lab.local" or "CN=User,OU=People,DC=lab,DC=local")
    /// </summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(
        maximumLength: 1024,
        MinimumLength = 1,
        ErrorMessage = "Username must be between 1 and 1024 characters.")]
    public required string Username { get; init; }

    /// <summary>
    /// Password (used for LDAP bind; never stored)
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(
        maximumLength: 256,
        MinimumLength = 1,
        ErrorMessage = "Password must be between 1 and 256 characters.")]
    public required string Password { get; init; }
}
```

### 3.3 Infrastructure Layer: LdapAuthenticationService

**Location:** `src/LabAuthServer.Infrastructure/Services/LdapAuthenticationService.cs`

**Purpose:** Implement LDAP bind authentication using `System.DirectoryServices.Protocols`

**Proposed behavior:**

- Attempt LDAP bind with provided username/password
- Translate LDAP exception codes to application-level errors
- Log authentication attempts (no passwords logged; no raw LDAP responses logged)
- Return `AuthenticationResult` to application layer

**Error handling:**

- Invalid credentials → "Authentication failed" (no "user not found" message)
- LDAP server unavailable → "Authentication service unavailable"
- Connection timeout → "Authentication request timed out"
- LDAP protocol error → "Authentication error"

### 3.4 Api Layer: AuthController

**Location:** `src/LabAuthServer.Api/Controllers/AuthController.cs`

**Purpose:** HTTP endpoint for authentication

**Proposed endpoint:**

```
POST /api/v1/auth/login
Content-Type: application/json

Request:
{
  "username": "user@lab.local",
  "password": "password123"
}

Response (200 OK):
{
  "authenticated": true
}

Response (401 Unauthorized):
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.3.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication failed."
}
```

---

## 4. Credential Handling & Security

### 4.1 Credential Input

- Username/password received as JSON in POST request body
- Transmitted over HTTPS only (enforced by Api middleware)
- Never persisted in database or source control

### 4.2 Credential Usage

- Passwords used **immediately** for LDAP bind operation
- **NOT stored** in memory after bind attempt
- **NOT logged** in any form
- **NOT included** in any audit trails or responses

### 4.3 Distinguished Name Resolution

The alternatives below are deferred and are not part of Phase 3:

**Option A: Simple username (sAMAccountName lookup)**
- User provides: `username`
- System queries LDAP for user object with `sAMAccountName=username`
- Gets distinguished name
- Performs bind with distinguished name + password
- **Pro:** User-friendly
- **Con:** Additional LDAP query; slight performance overhead
- **Implementation:** Add method to `ILdapService` for user lookup

**Option B: Assume UPN format (user@domain)**
- User provides: `user@lab.local`
- System uses the UserPrincipalName directly
- Performs bind
- **Pro:** Simpler; no additional queries
- **Con:** Requires user knowledge of format
- **Implementation:** Parse and validate UPN in authentication service

**Option C: Accept distinguished name directly**
- User provides: `CN=User,OU=People,DC=lab,DC=local`
- System performs bind with provided DN
- **Pro:** Most flexible
- **Con:** Least user-friendly
- **Implementation:** Minimal parsing

**Approved:** Use Option B with a UPN in the configured domain (`user@lab.local`) and pass that UPN directly to the LDAP bind. No DN is fabricated and no DN lookup is performed.

### 4.4 LDAP Bind Method

**Approved binding approach:**
- Direct user bind: `LdapConnection.Bind(new NetworkCredential(username, password))`
- **Pro:** Validates credentials; confirms password is correct
- **Con:** No password hashing; relies on LDAP server security
- **Security:** LDAPS provides TLS encryption; Windows certificate store validates server
- **Compliance:** Meets Coding Standard requirement "Use LDAPS with production certificate validation"

---

## 5. Error Handling & Logging

### 5.1 LDAP Exception Mapping

| LDAP Error Code | Message | HTTP Status |
|-----------------|---------|------------|
| 49 (InvalidCredentials) | "Authentication failed." | 401 |
| 81 (ServerDown) | "Authentication service unavailable." | 503 |
| Timeout | "Authentication request timed out." | 504 |
| Other | "Authentication error." | 500 |

Typed failure categories are retained through the Application contract so the API preserves the distinction between invalid credentials, directory outages, timeouts, and unexpected failures.

### 5.2 Logging Requirements

**Log these (without sensitive data):**
- Authentication attempt: `username` (no password)
- Result: success or failure (no reason details to log)
- Latency: time taken for LDAP operation

**Never log:**
- Passwords
- Raw LDAP responses
- Distinguished names (privacy concern)
- Authorization headers
- Tokens or secrets

**Example log entry:**
```
info: LabAuthServer.Infrastructure.Services.LdapAuthenticationService[0]
      Authentication attempt for user@lab.local completed in 250ms: Success
```

---

## 6. Testing Strategy

### 6.1 Unit Tests

**Location:** `tests/LabAuthServer.UnitTests/`

**Approved tests:**
- Authentication service tests use an Infrastructure LDAP client seam and do not contact a live DC.
- Tests cover valid `lab.local` UPNs, invalid domains, credential forwarding, failure mapping, strict LDAPS configuration, and cancellation.
- Controller tests use a fake `IAuthenticationService` and cover typed HTTP status mapping and HTTPS enforcement.

### 6.2 Integration Tests

**Location:** `tests/LabAuthServer.IntegrationTests/`

**Approved tests:**
- Controller behavior is isolated from the live DC.
- Live LDAP connectivity remains covered only by the separate RootDSE tests.

---

## 7. Configuration Requirements

### 7.1 Existing Configuration (Already Approved)

From `appsettings.json`:
```json
"ActiveDirectory": {
  "Domain": "lab.local",
  "Host": "DC01.lab.local",
  "Port": 636,
  "BaseDn": "DC=lab,DC=local",
  "UseLdaps": true,
  "ConnectionTimeout": "00:00:10"
}
```

### 7.2 Additional Configuration

⚠️ **REQUIRES ARCHITECT APPROVAL:**

**Option A: No additional configuration**
- Use existing `LdapOptions`
- Username format fixed to UPN or DN parsing
- **Pros:** Simple; minimal config
- **Cons:** Less flexible

**Option B: Add authentication-specific options**
```json
"Authentication": {
  "UsernameFormat": "upn",  // or "distinguishedName"
  "AllowEmptyPassword": false,
  "MaxPasswordLength": 256,
  "ConnectionTimeoutMs": 5000
}
```
- **Pros:** Flexible; explicit control
- **Cons:** Additional configuration to manage

**Superseded for directory read operations:** Root DSE and authenticated group lookup are separate concerns. User authentication continues to use the supplied UPN and password. Root DSE must not rely on anonymous bind because the target AD may reject it.

The Root DSE service account is configured through the existing `ActiveDirectory` options without storing a secret in source control:

```text
ActiveDirectory:ServiceAccountUsername
ActiveDirectory:ServiceAccountPasswordFile
```

Environment-variable equivalents use the standard double-underscore form:

```text
ActiveDirectory__ServiceAccountUsername
```

The LDAP service-account username remains a normal configuration value. The service-account password is not stored in appsettings, IIS environment variables, or source control. It is loaded by a Windows-only DPAPI-backed credential provider from the protected file at `C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi` and used only for the authenticated Root DSE bind. The password is never logged or returned, and the application fails closed if the secret file is missing or unreadable. Anonymous bind is not used for Root DSE.

Authentication continues to use the end-user credentials for the direct LDAPS bind and does not use the service account.

---

## 8. NuGet Package Requirements

**Already approved (implemented in Phase 2):**
- `System.DirectoryServices.Protocols` 10.0.0
- `Microsoft.Extensions.Logging` 10.0.0
- `Microsoft.Extensions.Options` 10.0.0

**No new packages are required for Phase 3 remediation:**

| Package | Version | Purpose | Required? | Status |
|---------|---------|---------|-----------|--------|
| `System.ComponentModel.Annotations` | 10.0.0 | `[Required]`, `[StringLength]` attributes | ✅ Yes | Likely bundled with ASP.NET Core |
| `System.DirectoryServices` | 10.0.0 | (Alternative to Protocols) | ❌ No | Using Protocols instead |

**Approved:** No new packages beyond the three already approved for Infrastructure.

---

## 9. Architectural Impact

### 9.1 Dependency Direction (Unchanged)

```
Domain
  ↑
Application (adds IAuthenticationService)
  ↑
Infrastructure (adds LdapAuthenticationService)
  ↑
Api (adds AuthController)
```

**Preserved:** No circular dependencies; no architecture violations

### 9.2 New Interfaces/Services

- `IAuthenticationService` in Application
- `LdapAuthenticationService` in Infrastructure
- `AuthController` in Api

**DI registration:**
```csharp
services.AddScoped<IAuthenticationService, LdapAuthenticationService>();
```

---

## 10. Approved Decisions

The following decisions were approved for Phase 3 and are implemented:

1. **UPN authentication:** Direct UPN bind for the configured `lab.local` domain. No fabricated DN and no lookup.

2. **LDAP bind approach:** Direct user bind over LDAPS/TCP 636.

3. **Error message granularity:** Generic invalid-credential response with no user enumeration.

4. **Configuration approach:** Existing `LdapOptions`; strict LDAPS/TCP 636 enforcement.

5. **NuGet package list:** No new packages beyond the approved Infrastructure packages.

6. **Testing strategy:** Authentication and controller tests are isolated from the live DC; RootDSE tests remain the live connectivity boundary.

---

## 11. Implementation Blockers & Risks

### Known Risks

- **DC Availability:** The application requires DC01.lab.local:636 at runtime; authentication tests do not.
- **User Account:** Live authentication verification requires an externally managed test account; no credentials are stored in the repository.
- **TLS Certificate:** DC certificate must be valid; Windows certificate store must include CA
- **Network:** Firewall rules must allow LDAPS (port 636)

### Mitigations

- Authentication tests use fakes and do not require live directory access
- No hard-coded test credentials in code
- Configuration supports non-production DC changes

---

## 12. Next Steps (Post-Approval)

Phase 3 implementation completed:

1. **Token Design** (Phase 4, awaiting separate approval)
   - JWT generation and validation
   - Token storage/transmission
   - Token expiration and refresh

2. **Authorization Design** (Phase 5, awaiting separate approval)
   - Access control policies
   - Role/scope management
   - API protection

3. **Database & Secrets** (Phase 6+, awaiting separate approval)
   - User profile persistence
   - Session/token storage
   - Secret management (API keys, certificates)

---

## 13. Compliance Checklist

Before implementation, verify:

- ✅ Architecture follows four-layer model
- ✅ No secrets in source code
- ✅ No passwords logged
- ✅ LDAPS with certificate validation (not bypassed)
- ✅ Async/await pattern used
- ✅ DTOs for input/output
- ✅ Dependency injection for all services
- ✅ ILogger<T> for structured logging
- ✅ No unapproved NuGet packages
- ✅ Tests added (unit + integration)
- ✅ Build succeeds with zero errors/warnings
- ✅ docs/Project_Status.md updated

---

## Appendix: Terminology

- **Distinguished Name (DN):** LDAP unique identifier (e.g., `CN=User,OU=People,DC=lab,DC=local`)
- **User Principal Name (UPN):** Email-like identifier (e.g., `user@lab.local`)
- **sAMAccountName:** Legacy username (e.g., `user`)
- **LDAP Bind:** Authentication operation (credentials validation)
- **LDAPS:** LDAP over SSL/TLS (encrypted)

---

## Document History

| Date | Author | Status | Notes |
|------|--------|--------|-------|
| 2026-08-30 | Codex (Implementation Agent) | Approved and implemented | Phase 3 authentication implementation and remediation |

---

**Status: APPROVED AND IMPLEMENTED**
