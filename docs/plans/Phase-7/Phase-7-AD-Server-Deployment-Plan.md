# Phase 7 — Lab AD Server Deployment

Date: 2026-09-20. Status: **PLANNING ONLY; TARGET CHECKS NOT RUN**. Executor: one developer (ALOT).

## Objective and scope

Deploy the existing v1.0.0 API package to the verified lab Windows/IIS environment, prove authentication and audit operation under its actual application identity, and retain a usable rollback. This document plans future execution; it does not authorize or perform infrastructure changes. No committees, legal gates, or renewed release qualification are required.

Do not rebuild, retag, replace the GitHub release, change source/dependencies, commit, push, or publish. Missing prerequisites become specific setup tasks; do not silently change authentication or database architecture. No infrastructure or application changes were made while preparing this plan.

## Current state and evidence

| Reference | Baseline |
| --- | --- |
| Release/tag | `v1.0.0`; existing GitHub release supplied as fact by the deployment brief; immutable |
| Source | `784fa96b9436aee315fae2d7e669a2650dbff794`; local tag resolves to this commit |
| Package | `LabAuthServer-1.0.0.zip`; framework-dependent API, recorded 51 files |
| SHA256 | `564f5be016e7f679c32751c4f30488b8482ca57ccec8207c81802a8aee73f6a0` |
| Retained local package | `C:\Apps\LabAuthServer\Releases\v1.0.0-preparation-20260913-212903\LabAuthServer-1.0.0.zip`; checksum independently matched during planning |
| Previous smoke evidence | `Smoke-Test-Report.md` beside that ZIP; final authentication rerun supersedes its earlier incomplete entries |

The final package smoke report records trusted HTTPS, health 200, anonymous/invalid-token 401, Reader login/access, Operator login/403 denial, DPAPI decryption, JWT issuance, encrypted SQL, correlated audit persistence, and LDAPS TLS. These checks used a temporary direct .NET process under Administrator, not the target IIS identity. They provide the test approach, not target acceptance. Prior build/default test evidence is 1,046 passing tests; no build or tests were rerun for this planning-only change.

Read [Project Status](../../Project_Status.md), [Validation Status](../../Validation_Status.md), [Architecture](../../Architecture.md), [SOP](../../internal/Coding_Standard_and_SOP.md), and [Development Plan](../../internal/Development_Plan.md). The latter two now reside under `docs/internal/`. Operational references: [Deployment](../../Deployment.md), [Configuration](../../Configuration.md), [Operations](../../Operations.md), [Authorization](../../Authorization.md), [Database](../../Database.md), and [Audit logging](../../AuditLogging.md).

Some summaries retain release-pending and older HSTS/licensing statements. This plan accepts the user's released baseline and checks behavior against the immutable source/package. In particular, the tagged `Program.cs` enables HSTS (one year, no includeSubDomains/preload) and initializes licensing with restricted-mode fallback. Do not disable these behaviors or introduce a licensing gate. No Phase 7 directory existed when inspected. No remote release or target infrastructure inspection was performed.

## Target architecture and constraints

Client HTTPS -> IIS/ASP.NET Core Module V2 -> in-process LabAuthServer API -> LDAPS directory and SQL audit procedure. JWT signing uses a Windows certificate-store RSA private key. Directory-query credentials come from a Windows DPAPI file. The package's `web.config` explicitly uses `dotnet`, `LabAuthServer.Api.dll`, and `hostingModel="inprocess"`; its runtime configuration requests `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` 10.0.0. A compatible serviced .NET 10 runtime and Hosting Bundle/ANCM are prerequisites; no SDK/build is needed on the target.

**Domain-controller decision:** the brief names a lab AD server and source examples mention a DC-style hostname, but neither proves the actual target's server role. Record its FQDN and Windows `DomainRole` before any setup. If it is a domain controller (role 4 or 5), explicitly record that fact. IIS/API exposure, extra runtime components, process privileges, resource contention, and machine-wide restart/configuration changes then affect a directory-critical host. Keep access limited to the intended lab clients, use a dedicated least-privilege pool identity, and avoid machine-wide IIS resets or unrelated service restarts. Do not run the API as Administrator/Domain Admin, assume local-account provisioning works on a DC, or change the AD server role. If the required hosting/identity setup cannot satisfy these constraints, record the concrete blocker before making changes.

Source hostnames, certificate thumbprints, SQL defaults, and prior pool identities are examples/evidence from another checkpoint, not selections for this target. Record actual values in a private deployment record; use placeholders in repository documentation.

## 1. Deployment preflight

Use prerequisite states **EXISTS**, **NEEDS CONFIGURATION**, **NEEDS OWNER ACTION**, **UNKNOWN**. EXISTS requires evidence for the actual target; old/local evidence alone is insufficient. NEEDS OWNER ACTION means a particular action requires credentials/access the developer does not hold, not a separate approval workflow. Record the missing action and person who can perform it; the developer handles everything within existing access.

| Prerequisite | Planning state | Future check / required record |
| --- | --- | --- |
| Immutable source and local ZIP | EXISTS locally | Tag/source match and hash above; verify again after target transfer |
| Target identity and DC status | UNKNOWN | FQDN, domain, OS edition/build/architecture, DomainRole, reachable management path |
| OS/.NET/IIS prerequisites | UNKNOWN | Confirm OS supports deployed runtime; installed .NET and ASP.NET Core 10 versions/architecture; IIS, WAS, ANCM V2 and Hosting Bundle; installation/restart needs |
| Site and pool | UNKNOWN | Names, current state, physical path, identity, authentication, bindings, inherited restrictions, other applications sharing the pool |
| Deployment/storage | UNKNOWN | Absolute versioned path, disk capacity for new ZIP/extraction plus backup/logs, deployment and evidence ACLs |
| Windows identity | UNKNOWN | Exact pool identity type/account/SID, logon viability, key/file access, SQL effective principal; never infer it from an Administrator smoke run |
| HTTPS certificate | UNKNOWN | Store, thumbprint, validity, SAN matching verified hostname, trusted chain/revocation, server-auth usage and hosting-layer private-key access |
| JWT certificate(s) | UNKNOWN | Configured store/thumbprints/key IDs, active private-key access for pool identity, previous public key/overlap metadata if configured |
| DNS/network | UNKNOWN | Client-to-hostname resolution and HTTPS binding/port; app-to-LDAPS 636 and actual SQL TCP port; DNS/time/trust/revocation dependencies; effective firewall rules |
| External settings | NEEDS CONFIGURATION | Assemble/review target-specific settings and precedence without copying source defaults wholesale; availability of an existing valid target configuration is UNKNOWN |
| DPAPI file | UNKNOWN | Absolute path, existence, nonempty file, target-machine protection provenance, ACL and runtime decryption |
| Directory | UNKNOWN | Trusted LDAPS hostname/TLS, domain/UPN suffix, search bases, service-account search/read and bind access |
| Groups/test users | UNKNOWN | Mapped groups plus enabled Reader and Operator (or Administrator) test users with unique searchable UPNs and appropriate direct memberships |
| SQL/audit | UNKNOWN | Trusted encrypted SQL connection, database/catalog/procedure presence and runtime procedure-execution permission; separate read access for evidence |
| Logs/rollback | UNKNOWN | Writable restricted log location, available IIS/ANCM diagnostics, prior package/settings backup and recovery path |

During execution, replace UNKNOWN with evidence or an actionable missing prerequisite. If an install, DNS/certificate/firewall change, AD account/group setup, or SQL provisioning is needed, enumerate that specific task first. No blanket schema/group setup and no trust bypasses.

Safe read-only examples to run later **on the confirmed target**, not evidence already collected:

```powershell
Get-CimInstance Win32_ComputerSystem | Select-Object Name, Domain, DomainRole
Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, OSArchitecture
dotnet --list-runtimes
```

Inspect existing IIS settings and file/key ACLs using available administrative tools; do not dump secret-bearing configuration or credentials into evidence. No install or binding commands are supplied before actual target values are known.

## 2. Artifact acquisition

1. Obtain the existing release asset from the already-published v1.0.0 release, or use the verified retained copy above. Record its acquisition source and UTC time. Do not fetch a source archive as a substitute.
2. Preserve the original ZIP read-only in controlled storage. Recompute SHA256 after transfer; a mismatch stops deployment.
3. Extract into a new, empty, versioned directory such as `<DEPLOYMENT_ROOT>\v1.0.0-<UTC_STAMP>`. Confirm the ZIP entries stay inside that directory before extraction. Never extract over the running version.
4. Compare every extracted artifact file hash with the verified ZIP contents; retain a manifest. Confirm `web.config`, runtime/dependency files and DLLs exist. Keep external configuration additions separately identified.
5. Record source SHA, tag, asset name, ZIP checksum, extracted manifest and deployment path together. Do not modify package binaries or regenerate the ZIP.

Deterministic checksum example using the known local file (run again at the verified target location after transfer):

```powershell
$phase7ZipPath = 'C:\Apps\LabAuthServer\Releases\v1.0.0-preparation-20260913-212903\LabAuthServer-1.0.0.zip'
$phase7Expected = '564f5be016e7f679c32751c4f30488b8482ca57ccec8207c81802a8aee73f6a0'
$phase7Actual = (Get-FileHash -LiteralPath $phase7ZipPath -Algorithm SHA256).Hash
if ($phase7Actual -ne $phase7Expected) { throw 'Artifact SHA256 mismatch; stop deployment.' }
$phase7Actual
```

## 3. External configuration

The ZIP excludes environment appsettings. Maintain the authoritative settings outside the artifact/source tree in restricted storage. For the normal IIS content-root model, place a controlled deployment copy of `appsettings.json` beside the deployed DLL, separately tracked from the package manifest. An arbitrary sibling JSON file is not automatically loaded. Preserve any existing verified content-root arrangement; do not reuse the earlier loopback Kestrel smoke settings.

| Section | Required review |
| --- | --- |
| `ActiveDirectory` | `Domain`, `Host`, `Port=636`, `BaseDn`, `UserSearchBaseDn`, `ServiceAccountUsername`, absolute `ServiceAccountPasswordFile`, `UseLdaps=true`; `ConnectionTimeout`, `AuthenticationTimeout` (1–60 seconds), `MaxConcurrentLdapOperations` (1–32), `MaxPendingLdapWaiters` (1–128), `MaximumGroupMemberships` |
| `Token` | HTTPS `Issuer`, intended `Audience`, `AccessTokenLifetime`, `ClockSkew`, supported `SigningAlgorithm`, `ActiveKeyId`, `ApprovedKeyIds` where used, `SigningCertificateStoreLocation`, `SigningCertificateStoreName`, `SigningCertificateThumbprint`, `SigningKeyStoreReference`; optional `PreviousKeyId`, `PreviousSigningCertificateThumbprint`, `PreviousKeyExpiresAt` |
| JWT limits | Preserve release-compatible `MaximumClaimSize=4096` and `MaximumTokenSize=7680`; encoded JWT 12288 and Authorization value 12352 are fixed code limits, not settings |
| `Authorization` | `DefaultDeny=true`, bounded `MaximumGroupCount`, health/login `PublicEndpoints`, verified `GroupToRoleMappings` |
| `Audit` | Target `ConnectionString` using `Encrypt=True;TrustServerCertificate=False`, correct database/server name and authentication method; `CommandTimeoutSeconds` >0 and <=60; procedure name is fixed in implementation, not a configurable setting |
| Hosting | Exact `AllowedHosts`, Production environment, correct IIS site root and HTTPS port; inspect `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT`, environment-section overrides and command-line settings for conflicts |
| Logging | `Logging:LogLevel`; restricted operational log destinations/diagnostic capture; no credentials, tokens or request-body logging |

JWT signing certificate policy: currently valid RSA 2048–4096, compatible signing KeyUsage, no EKU extension, unambiguous thumbprint match; active signing key requires private-key access. HTTPS and LDAPS server certificates have different purposes and validation requirements; do not assume the HTTPS certificate can serve as the JWT certificate. Previous-key overlap must still be valid at startup if configured.

For in-process IIS, the public HTTPS listener/certificate belongs to IIS, not a new Kestrel HTTPS endpoint. Review any `Kestrel`, URL, or HTTPS-port overrides inherited from earlier direct-process smoke testing. Do not transplant loopback port 18443. Verify redirect behavior against the actual binding. Preserve released HSTS and existing proxy trust; do not add a proxy or host-limit tuning in this deployment.

## 4. Windows identity and DPAPI

Before cutover, fill these exact fields in the private record: `<IIS_SITE>`, `<IIS_APP_POOL>`, identity type, `<APP_IDENTITY>` and SID, `<SQL_EFFECTIVE_PRINCIPAL>`, directory service-account reference, `<SECRET_FILE>`, certificate store/key references. The pool process identity, LDAP query service account and submitted login user are distinct identities.

The tagged credential provider uses `ProtectedData.Unprotect(..., optionalEntropy: null, scope: LocalMachine)`. This is **machine-bound**, not CurrentUser protection. A file copied from another machine is not sufficient; it must have been protected for this target. Its protection does not require the original protecting user, but filesystem ACLs are the access boundary. The actual pool process must be able to read/decrypt it. Do not switch DPAPI scope or grant broad Users/Everyone access. If target-local protection is missing, record a prerequisite to provision it securely on that machine without displaying the password.

Verify least-privilege read/execute for binaries, read for configuration and secret, private-key use for JWT signing, write only to required log directories, and the intended SQL authentication/permissions. Confirm the IIS hosting layer can use its TLS key. For a CurrentUser certificate store, verify the actual pool identity's store/profile, not the developer's. Record profile-loading requirements only if the selected store/identity needs them; LocalMachine DPAPI alone does not imply a user-profile requirement.

A successful fresh real-user login through IIS proves the deployed DPAPI path and signing access; startup/health alone cannot. Do not use an Administrator-only decrypt probe as substitute evidence.

## 5. IIS / HTTPS deployment design

Use a dedicated pool for this application; preserve the verified pool identity and settings where suitable. Proposed new-pool settings are No Managed Code, Integrated pipeline, and bitness matching installed runtime/ANCM. Record existing start mode, idle timeout and recycle settings; select the required startup behavior without assuming Application Initialization is installed. Verify a cold start and a controlled recycle followed by health/login.

Use a dedicated site root unless an existing verified arrangement preserves `/api/v1/...` paths. Record site/application name, versioned physical path, HTTPS IP/port/hostname/SNI and certificate. Keep IIS anonymous authentication available so the API's public login/health and bearer flow work; inspect inherited Windows-auth restrictions rather than enabling a different authentication architecture.

Retain the packaged in-process ANCM configuration. In-process native limits are not controlled by Kestrel settings; verify the actual route and representative issued token sizes. Do not increase machine-wide limits to force a test through.

The application configures Console/Debug logging; it does not provide a rolling file logger. Packaged ANCM stdout logging is disabled. Record how IIS access logs, Windows ANCM startup events, and application console diagnostics will be collected. If temporary ANCM stdout capture is needed during execution, restrict its directory, record the external hosting override, and disable it after diagnosis. Do not claim console logs are durably retained without testing capture. SQL audit is separate and best effort.

## 6. Database and AD verification

Perform read-only prerequisite checks before cutover; later smoke requests intentionally append normal audit events and do not change schema or directory membership.

- Under the effective application SQL identity, establish a trusted encrypted connection to the selected database. Confirm encryption/session evidence through available read permissions; do not grant server administration just to inspect diagnostics.
- Verify `Reference.EventTypes`, `Audit.AuditEvents`, `Audit.usp_WriteAuditEvent`, enabled event catalog entries, and `LabAuthServer_AuditWriter` (or equivalent reviewed execute-only grant). The app needs procedure execution, not direct table writes, DDL, or db_owner. Use separately authorized read access to inspect smoke evidence.
- If an object/permission is missing, identify exactly which prerequisite is absent. Review the existing `database/Phase11/` scripts for that target; never run the create-database/schema scripts blindly. Schema changes are not part of normal v1.0.0 deployment.
- Validate LDAPS TLS hostname/chain and TCP 636, then service-account search/read and user bind through the actual login path. Port reachability alone is insufficient.
- Verify enabled test accounts have populated, unique UPNs within the configured search base. Prior smoke failures included a user without a searchable UPN; do not repeat that as an authorization test.
- Confirm direct memberships map as configured: `GG-APP-USER`/`GG-APP-REPORT` -> Reader, `GG-APP-APPROVER` -> Operator, `GG-APP-ADMIN` -> Administrator. These are repository mappings; target group existence is unverified. No nested-group expansion is implemented.
- Only the highest role is issued: Administrator > Operator > Reader. Policies check exact roles, not hierarchical access: Operator and Administrator tokens receive 403 at the Reader resource. A user with no mapped role cannot supply a valid non-Reader authorization test.

## 7. Controlled deployment sequence

One developer executes and records each step in order during the later deployment session:

1. Complete preflight; record unresolved prerequisites. Capture baseline health/HTTPS and current site/pool state. Identify any current deployment or explicitly record first installation.
2. Back up the current complete application directory and matching external settings with ACLs, original ZIP/hash/version if known, IIS site/pool/bindings/environment settings, relevant configuration inheritance, and certificate/key/DPAPI references. Keep secret-bearing backups restricted and outside Git. Do not export private keys as a routine backup step. Confirm rollback resources remain usable on this machine.
3. Stage the checksum-verified ZIP into the new versioned path and verify its file manifest. Retain the old directory untouched.
4. Stop only the application/pool being replaced, if applicable, once backups/staging are ready. Avoid `iisreset` and unrelated application interruption.
5. Apply the reviewed external settings and least-privilege access to the staged directory. Preserve package file hashes; document any required hosting override separately.
6. Point the verified IIS site/application at the staged path; apply only the recorded hosting/binding changes needed for this target. For first installation, record every newly created object for rollback.
7. Start the pool/site and inspect startup diagnostics. Confirm the intended Production environment and running identity without dumping configuration.
8. Verify normal client HTTPS certificate/hostname validation through the intended route, then health 200/Healthy.
9. Execute the authentication/authorization checklist below with temporary in-memory credentials/tokens and the actual IIS identity.
10. Verify correlated SQL audit rows and safe logs; an HTTP success does not prove audit persistence.
11. Complete controlled recycle/restart verification and the rollback exercise if practical. If rollback is not exercised, explicitly mark it NOT RUN with reason and follow-up; do not label it tested.
12. Capture final evidence, exact deployed path/checksum, outcome and any remaining errors. Stop or roll back for a required failure; do not repair source or replace the immutable release during deployment.

## 8. Post-deployment smoke checklist

Use the proven package smoke approach against `<VERIFIED_HTTPS_BASE_URL>` through IIS. Enter test credentials via a permitted local interactive client, never shell arguments, saved request collections, transcripts, screenshots or repository files. Keep passwords/JWTs in memory and discard them after testing. Use fresh `X-Correlation-ID` GUIDs and record only safe outcomes. Stay below the shared 10-login-per-minute rate limit; a 429 is not an authentication result.

Result vocabulary: **PASS** = expected result observed; **FAIL** = executed but wrong result; **NOT RUN** = not executed or prerequisite missing; **NOT APPLICABLE** = capability absent or condition genuinely absent, with reason. All target checks start NOT RUN; missing test users do not make a supported test NOT APPLICABLE.

| Check | Expected result | Initial result |
| --- | --- | --- |
| HTTPS on actual client route | Normal trust, hostname and validity checks pass; no `-k`/trust bypass | NOT RUN |
| Startup / cold start / recycle | Correct pool identity/configuration; stable startup, repeat health and Reader login after recycle | NOT RUN |
| `GET /api/v1/health` | 200 and `status: Healthy`; liveness only, not AD/SQL readiness | NOT RUN |
| Anonymous `GET /api/v1/protected` | 401, Bearer challenge; no audit row required for anonymous 401 | NOT RUN |
| Invalid bearer token at protected route | 401 and correlated `SEC_INVALID_TOKEN` audit event | NOT RUN |
| Reader `POST /api/v1/auth/login` | 200 using real enabled UPN/password; exercises LDAPS service/user operations and DPAPI under IIS identity | NOT RUN |
| Issued Reader JWT | Expected issuer/audience, `kid`, times and required claim names; role Reader; endpoint accepts signature; never retain token/identity values | NOT RUN |
| Reader token at protected route | 200; record correlation and safe response outcome | NOT RUN |
| Operator (or mapped Administrator) login | 200 and expected highest mapped role; no-role account is unsuitable | NOT RUN |
| Non-Reader token at Reader route | 403 and correlated `AUTHZ_ACCESS_DENIED` | NOT RUN |
| Operator-only / Administrator-only successful resource | No such exposed endpoints; named policies alone do not create routes | NOT APPLICABLE |
| SQL authentication audit | Correlated `AUTH_LOGIN_SUCCESS` for Reader/non-Reader logins; role/status agree with HTTP evidence | NOT RUN |
| Diagnostic review | No unresolved startup, DPAPI, key, LDAPS or audit persistence errors; timestamps/correlation support checks | NOT RUN |
| Host/response/transport checks | Intended AllowedHosts accepted; no-store/nosniff/DENY and released HSTS observed; normal issued tokens traverse actual route; inspect relevant HTTP versions/hops | NOT RUN |
| HTTP redirect | Correct HTTPS destination if an HTTP binding exists; otherwise NOT APPLICABLE with binding evidence | NOT RUN |

If a safe failed-login check is performed with a designated test account, confirm the expected authentication failure and correlated event without approaching AD lockout thresholds. Do not require a failing-password attempt for completion. Do not assume a JWT-issued event exists: token success is evidenced by login and subsequent signature-validated resource access. Retain audit history; no test cleanup deletes rows.

## 9. Solo-developer rollback

Trigger rollback when startup, HTTPS, real login, applicable role behavior or audit verification fails and cannot be corrected within the recorded deployment window, or when this deployment disrupts the shared host. The same developer performs recovery; no committee is required.

1. Record failure time, safe diagnostics and correlation IDs. Stop only the affected application/pool.
2. For an upgrade, switch the site's physical path back to the preserved previous directory and restore its matching settings/ACLs and recorded site/pool/environment/binding changes. Identify the old artifact by its captured checksum and source/version where known; do not call an unidentified backup a known release.
3. Restore prior identity/key/secret access only where changed and recorded. Keep DPAPI on its original machine. Do not delete certificates, revoke keys, purge audits or restore the whole SQL database as an application rollback. No schema migration is expected.
4. Start the previous pool/site and verify HTTPS, health, Reader login/access, anonymous 401, applicable non-Reader 403 and correlated audit persistence against the previous baseline. Record deviations rather than assuming an older build has identical behavior.
5. For first installation with no prior app, stop the new site/pool and restore the captured absence of application/bindings and any recorded prerequisite changes where safely reversible. Do not uninstall shared IIS/.NET components or disturb AD/SQL. Mark previous-version restoration NOT APPLICABLE; verify pre-existing host services still meet the captured baseline.
6. Record restored path/configuration version, prior artifact checksum, site/pool state, UTC times, HTTP/correlation/audit evidence, and rollback result. Retain the failed version for investigation. If rehearsing rollback then returning to v1.0.0, repeat the complete required smoke checks after redeployment.

Test rollback before calling it tested. A documented but unexercised procedure is explicitly **NOT RUN**, with reason and a proposed exercise window. Lack of a usable backup/recovery path blocks cutover; an unexercised documented rollback is a disclosed limitation allowed by this phase's completion criteria.

## 10. Evidence, blockers and completion

Keep one private deployment record with: target/DC status; prerequisite states; source/tag/ZIP hash; extraction manifest; paths and backup identifiers; runtime/ANCM versions; actual process identity; sanitized configuration revision and override inventory; certificate metadata and access checks; LDAPS/SQL trust evidence; UTC test results and correlation IDs; minimal redacted audit results; startup/log observations; rollback steps and test status. Do not put real login identities, connection secrets, passwords, JWTs, private keys or decrypted DPAPI contents in the repository or shared report.

Current execution blockers are **unverified prerequisites**, not demonstrated application defects: target identity/DC role, hosting/runtime availability, actual pool identity and permissions, target-local DPAPI protection, trusted HTTPS/JWT/LDAPS/SQL resources, external configuration, mapped test accounts, audit permissions/evidence access, and a recoverable current-state backup. Prior final package smoke passed but does not close these target checks. The exact GitHub asset URL/acquisition source also needs recording if using a remote download; remote metadata was not inspected during planning.

Deployment is complete only when the exact v1.0.0 package/hash is recorded; application startup and trusted HTTPS pass; health returns 200; real Reader and applicable non-Reader flows prove DPAPI, LDAPS, JWT and role behavior under the IIS identity; required correlated SQL events exist; no critical deployment errors remain; and rollback is documented with PASS or explicitly NOT RUN plus reason. Unsupported role-specific endpoints remain NOT APPLICABLE. A required smoke check that is FAIL or NOT RUN prevents completion. Report the rollback limitation alongside any completion claim.

Planning validation: repository documentation and immutable source/package metadata inspected; local tag and retained ZIP checksum matched. No deployment, installation, IIS/certificate/AD/SQL/firewall/DNS modification, source change, rebuild, tag/release change, commit, push or publication was performed. Build/unit/integration tests are NOT RUN for this documentation-only planning task; the user's no-rebuild requirement is preserved.
