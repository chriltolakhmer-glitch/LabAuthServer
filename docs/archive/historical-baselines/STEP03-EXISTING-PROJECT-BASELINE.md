# STEP 3 â€” EXISTING LABAUTHSERVER PROJECT BASELINE

## Development Environment
- Development PC: <DEVELOPMENT_HOST>
- Target Server: <HOST>
- Target Domain: <DOMAIN>
- Target AD/DC FQDN: <LDAP_HOST>
- Target IIS Site: LabAuthServer
- Target App Pool: <IIS_APP_POOL>

## Existing Project
- Solution: `LabAuthServer.slnx`
- Solution path: `<REPOSITORY_PATH>\LabAuthServer.slnx`
- Project: `LabAuthServer.Api`
- Project path: `<REPOSITORY_PATH>\src\LabAuthServer.Api\LabAuthServer.Api.csproj`
- Framework: `net10.0`
- ASP.NET Core: ASP.NET Core Web SDK / .NET 10 Web project
- SDK: `10.0.400` via `global.json`
- Main source folders:
  - `src/LabAuthServer.Api/`
  - `src/LabAuthServer.Application/`
  - `src/LabAuthServer.Domain/`
  - `src/LabAuthServer.Infrastructure/`
  - `tests/LabAuthServer.UnitTests/`
  - `tests/LabAuthServer.IntegrationTests/`

## Configuration Files Present
- `global.json`
- `LabAuthServer.slnx`
- `src/LabAuthServer.Api/appsettings.json`
- `src/LabAuthServer.Api/appsettings.Development.json`
- No `src/LabAuthServer.Api/appsettings.Production.json` present in the source tree
- `NuGet.Config`
- Project-level `*.csproj` files for all projects

## Existing Authentication
The existing application implements:
- ASP.NET Core authentication pipeline with `AddAuthentication` and `AddJwtBearer`
- HTTPS redirect via `app.UseHttpsRedirection()`
- Anonymous health endpoint at `/api/v1/health`
- LDAP direct-bind authentication flow over LDAPS/TCP 636
- Authentication failure mapping to 401/503/504/500 categories
- Application-level authorization policy registration with default authenticated user policy and named role policies

Observed implementation:
- `src/LabAuthServer.Api/Program.cs` configures `AddProblemDetails()`, controllers, AD options, and token configuration.
- `src/LabAuthServer.Api/Extensions/TokenConfigurationExtensions.cs` binds `TokenOptions` and `AuthorizationPolicyOptions`, adds JWT bearer auth, registers `IProtectedSigningKeyProvider`, `ITokenSigningService`, `ITokenService`, and named policies.
- `src/LabAuthServer.Api/Extensions/JwtBearerAuthenticationOptions.cs` validates JWT issuer/audience/algo/time and resolves keys via the protected key provider.
- `src/LabAuthServer.Infrastructure/Services/LdapAuthenticationService.cs` and `LdapAuthenticationClient.cs` enforce LDAPS port 636 and protocol version 3.

## Existing LDAP/LDAPS
Observed source values:
- Domain: `<DOMAIN>`
- Host: `<LDAP_HOST>`
- Port: `636`
- BaseDn: `<BASE_DN>`
- UseLdaps: `true`
- ConnectionTimeout: `00:00:10`

Implementation notes:
- LDAP configuration is centrally bound from `ActiveDirectory` settings.
- `LdapAuthenticationClient` rejects non-LDAPS or non-port-636 usage.
- Root DSE queries are available through `LdapService` using `System.DirectoryServices.Protocols`.
- This is consistent with the approved Phase 3 LDAPS requirement and with the target environment.

## Existing AD Group Mapping
The source currently contains authorization model and mapping infrastructure, but the current production configuration does not declare the approved Step 3 values in `appsettings.json`.

Observed current mapping model:
- `AuthorizationPolicyOptions.GroupToRoleMappings`
- `AuthorizationPolicyOptionsValidator` enforces default deny, unique mapping keys, and bounded values.
- `AdGroupRoleMappingService` resolves groups into application roles using an allowlist.

Observed current default config:
- `Authorization.GroupToRoleMappings` is currently empty in `appsettings.json`.
- There are no hard-coded approved Step 3 group definitions in the checked-in config.

Approved Step 3 values:
- `GG-APP-ADMIN    -> Administrator`
- `GG-APP-APPROVER -> Operator`
- `GG-APP-USER     -> Reader`
- `GG-APP-REPORT   -> Reader`

Current implementation status:
- Infrastructure supports the concept of group-to-role mapping.
- The checked-in config does not currently contain the approved Step 3 mappings.

## Existing JWT
Observed current values:
- Issuer: `https://labauthserver.local`
- Audience: `labauthserver-api`
- Access token lifetime: `00:15:00`
- Clock skew: `00:05:00`
- Signing algorithm: `RS256`
- Active key id: `development-key-1`
- Signing key store reference: `<KEY_STORE_REFERENCE>`
- Maximum claim size: `4096`
- Maximum token size: `16384`

Approved Step 3 values:
- D3.3 JWT issuer: `https://<LDAP_HOST>`
- D3.4 JWT audience: `LabAuthServer.API`

Current implementation status:
- JWT bearer authentication is implemented in the API layer.
- JWT validation is configured and validated through `TokenOptionsValidator` and `JwtBearerAuthenticationOptions`.
- The current issuer and audience values differ from the approved Step 3 values.
- The current signing key reference is an environment placeholder, not the approved Windows Certificate Store `LocalMachine\My` thumbprint value.

## Existing Authorization
Observed implementation:
- Authorization policies exist for:
  - `RequireReader`
  - `RequireOperator`
  - `RequireAdministrator`
- Default policy requires an authenticated user.
- `PublicEndpoints` currently include:
  - `/api/v1/health`
  - `/api/v1/auth/login`
- Default deny is enabled.

Observed source-level role constants:
- `Reader`
- `Operator`
- `Administrator`

Status:
- Authorization model exists.
- The approved Step 3 group map is not yet reflected in the source configuration.

## Build Result
Build commands executed on the development PC:
- `dotnet restore`
- `dotnet build LabAuthServer.slnx --configuration Release --no-restore`

Result:
- Restore complete
- Build succeeded
- Target framework: `net10.0`
- SDK: `10.0.400`

## Security Review
Findings by severity:

CRITICAL:
- None found in the read-only source review from the codebase itself.
- No hard-coded password, secret, private key, or certificate material was observed in the checked-in source.

HIGH:
- JWT issuer value differs from the approved Step 3 environment value (`https://labauthserver.local` vs `https://<LDAP_HOST>`).
- JWT audience differs from the approved Step 3 value (`labauthserver-api` vs `LabAuthServer.API`).
- Signing key store configuration does not match the approved Windows Certificate Store value; source currently has `<KEY_STORE_REFERENCE>` rather than the approved certificate thumbprint or Store/My binding.
- AD group mapping values are not populated with the approved Step 3 group map in the checked-in config.

MEDIUM:
- There is no `appsettings.Production.json` file in the source tree; a deployment-time production config must still be provisioned for the target server environment.
- The existing code sets production-style JWT validation, but the runtime environment configuration is not yet aligned with the approved Step 3 deployment values.

LOW:
- Logging is intentionally console/debug only and not excessive; however, the project logs operational LDAP failures and should be reviewed for sensitivity before production exposure.

INFO:
- The project already contains a clean layered architecture and the expected core components.
- The app is not yet fully aligned with the approved Step 3 deployment values, but the source is structurally ready for a deployment readiness review.

## Deployment Readiness
This is a development-PC discovery and readiness assessment only; no deployment action was performed.

Recommended publish flow:
1. On the development PC, run `dotnet publish LabAuthServer.slnx -c Release -o .\publish` or publish the API project specifically.
2. Copy the published output to the target IIS host `<HOST>`.
3. Configure the IIS site `LabAuthServer` and app pool `<IIS_APP_POOL>` on the target server, using the approved HTTPS certificate and environment-specific configuration.
4. Ensure the environment is configured for the target AD/DC values.

Required deployment prerequisites on <HOST>:
- ASP.NET Core Hosting Bundle / runtime for .NET 10
- IIS site and app pool configured for `LabAuthServer`
- HTTPS binding on port 443
- Certificate matching the approved thumbprint `<THUMBPRINT>`
- App service identity with rights to read the certificate and application files
- Access to `<LDAP_HOST>:636` over LDAPS
- Environment variables or configuration values aligned with the approved deployment settings

## Gaps
- Issuer does not match the approved D3.3 value.
- Audience does not match the approved D3.4 value.
- AD group mappings do not match the approved mapping requiremen ts.
- Signing key store reference does not match the approved certificate-store model.
- Production configuration is not yet aligned with the target deployment environment.
- The project is build-ready but not yet deployment-ready against the approved Step 3 values.

## Required Changes Before Deployment
The following must be aligned before deployment to target server `<HOST>`:
- JWT issuer to `https://<LDAP_HOST>`
- JWT audience to `LabAuthServer.API`
- AD group mapping entries to the approved list
- Signing key reference to the approved Windows Certificate Store / certificate thumbprint
- Target environment configuration for IIS/host binding and runtime settings
- Appsettings or environment variables for production deployment values

## Deployment Plan
Development PC (<DEVELOPMENT_HOST>)
- `dotnet publish` the existing application in Release mode
- Publish output to a dedicated folder such as `publish/`
- Transfer the published files to the target server

Target Server (<HOST>)
- Configure IIS site `LabAuthServer`
- Configure app pool `<IIS_APP_POOL>`
- Bind HTTPS on port 443 using the existing certificate thumbprint `<THUMBPRINT>`
- Place the published app under the site root
- Configure environment variables or appsettings overrides for the approved values
- Validate connectivity to `<LDAP_HOST>:636` via LDAPS

## Safety
Confirmed read-only phase:
- No target server changes
- No AD changes
- No IIS changes
- No certificate changes
- No firewall changes
- No source changes during discovery
- No publish/deploy action performed

## Summary
The existing LabAuthServer source on the development PC is valid, builds successfully, and already contains the expected ASP.NET Core, LDAPS, JWT, and authorization infrastructure. However, the current checked-in config does not match the approved Step 3 deployment values for issuer, audience, AD group mappings, and signing-key configuration. The app is therefore build-ready but not yet aligned with the approved deployment baseline for <HOST>.

# PHASE 4 â€” CONFIGURATION ALIGNMENT

## Changes Made

### File: `src/LabAuthServer.Api/appsettings.json`
- Before:
  - `Token.Issuer`: `https://labauthserver.local`
  - `Token.Audience`: `labauthserver-api`
  - `Authorization.GroupToRoleMappings`: `{}`
- After:
  - `Token.Issuer`: `https://<LDAP_HOST>`
  - `Token.Audience`: `LabAuthServer.API`
  - `Authorization.GroupToRoleMappings`:
    - `GG-APP-ADMIN` -> `Administrator`
    - `GG-APP-APPROVER` -> `Operator`
    - `GG-APP-USER` -> `Reader`
    - `GG-APP-REPORT` -> `Reader`
- Reason:
  - Align the existing active configuration with the approved Step 3 values without changing the source architecture.
- Approved Decision:
  - D3.3 JWT Issuer
  - D3.4 JWT Audience
  - D3.5 AD group-to-role mapping

## JWT Configuration
- Issuer: `https://<LDAP_HOST>`
- Audience: `LabAuthServer.API`
- Lifetime: `00:15:00` (unchanged; no explicit D3.9/3600-second approval was found in the approval record, so no automatic lifetime change was made)
- Algorithm: `RS256`
- Signing key mechanism: existing abstraction remains `<KEY_STORE_REFERENCE>`, but this does not yet satisfy the approved `Windows Certificate Store` requirement.

## LDAP/LDAPS
- Host: `<LDAP_HOST>`
- Port: `636`
- Base DN: `<BASE_DN>`
- LDAPS enabled: `true`

## AD Group Mapping
- `GG-APP-ADMIN` -> `Administrator`
- `GG-APP-APPROVER` -> `Operator`
- `GG-APP-USER` -> `Reader`
- `GG-APP-REPORT` -> `Reader`

## Build Result
- Command executed: `dotnet restore` and `dotnet build LabAuthServer.slnx --configuration Release --no-restore`
- Result: PASS
- Target framework: `net10.0`
- Configuration: `Release`
- Output location: `src/LabAuthServer.Api/bin/Release/net10.0/`

## Test Result
- Command executed: `dotnet test LabAuthServer.slnx --configuration Release --no-build`
- Result: PASS (unit and integration tests ran successfully in the current local environment)

## Publish Result
- Not executed. The project was prepared for publish-readiness review only; no publish was performed to the target server or to a production directory.

## Security Review
- No hard-coded passwords, TLS bypass, or source-stored private keys were introduced in this configuration pass.
- Remaining blocker: the certificate-store signing requirement is not yet implemented in the existing abstraction. The current code resolves a generic key provider and does not yet acquire the certificate from `Cert:\LocalMachine\My` by thumbprint.
- Remaining blocker: the approval record does not explicitly define a one-hour lifetime decision (`D3.9`), so the existing 15-minute lifetime remains unchanged pending explicit approval.

## Remaining Gaps
- The signing abstraction must be updated to obtain the X509 certificate from `Cert:\LocalMachine\My` using thumbprint `<THUMBPRINT>`.
- The current code does not yet support certificate-store lookup cleanly and therefore cannot satisfy Step 5 without redesigning the existing signing abstraction.
- No explicit one-hour lifetime decision was found in the authoring set; the lifetime remains a pending approval item.

## Deployment Prerequisites
- ASP.NET Core Hosting Bundle for .NET 10 on target IIS server
- IIS site and app pool `LabAuthServer` / `<IIS_APP_POOL>`
- HTTPS binding on port 443 with certificate thumbprint `<THUMBPRINT>`
- LDAPS connectivity to `<LDAP_HOST>:636`
- Environment-based configuration for deployment-specific values to avoid source-controlled secrets

## Target Server Deployment Plan
- Use the development PC to publish the existing API project in Release mode to a local folder.
- Transfer the published files securely to the target server `<HOST>`.
- Configure the IIS site and application pool only after approval.
- Bind HTTPS using the approved certificate thumbprint.
- Configure deployment-specific settings through environment variables or machine-level configuration rather than source-controlled appsettings.

## Safety Verification
- Target server <HOST> unchanged: confirmed
- AD unchanged: confirmed
- DNS unchanged: confirmed
- Firewall unchanged: confirmed
- IIS unchanged: confirmed
- Certificate unchanged: confirmed
- No private key exported: confirmed

## Final status
The configuration alignment is partial and blocked only at the certificate-store signing implementation. The project remains build-ready and test-ready on the development PC, but the certificate-store signing requirement and the explicit token-lifetime decision remain open approval items before deployment.
