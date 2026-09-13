> Historical multi-owner workflow, superseded 2026-09-13 by the [solo developer release checklist](../../plans/Phase-6/Release-Checklist.md). Original decisions, pending items, and results below are retained as history, not future release gates.

# Phase 6.6 — Target Environment Acceptance

Status: ACCEPTANCE DESIGN AND EVIDENCE PACKAGE; NO TARGET ENVIRONMENT ACCEPTED.

## 1. Purpose

Target-environment acceptance proves that an authorized environment can host and operate the currently implemented LabAuthServer behavior. It connects the repository contract to evidence from the selected Windows host, IIS site, SQL Server, directory path, certificates, runtime identity, and operational owners.

Configuration validation, automated tests, a successful build, or a liveness response do not by themselves accept an environment. An environment is not accepted unless the evidence listed in this document is complete, reviewable, attributable to the intended target, and free of exposed secrets.

This phase is an acceptance design and evidence package only. It does not perform deployment, install or change IIS, change AD/domain configuration, change SQL production objects, generate or install certificates, create a release, onboard a customer, or modify Phase 5.6.

## Current implementation assumptions

The current repository establishes these assumptions before target evidence is collected:

| Boundary | Observed implementation behavior | Evidence limit |
| --- | --- | --- |
| Hosting | ASP.NET Core .NET 10 API is published for Windows/IIS using `AspNetCoreModuleV2` and in-process hosting. | Publish files and source prove the intended model, not a running target site. |
| Configuration | Options are bound and validated at startup. Sensitive values are expected from protected host configuration or Windows-protected files. | Startup success does not prove target values, permissions, reachability, or secret decryption. |
| Directory | LDAPv3 is used through `System.DirectoryServices.Protocols`; production requires LDAPS, normally TCP 636. Service-account credentials are loaded through Windows DPAPI. | Automated fakes and Root DSE acceptance do not prove real-user login or group authorization. |
| SQL | Audit events use `Audit.usp_WriteAuditEvent` with typed parameters and a bounded command timeout. Persistence is best effort; failures are logged and return no audit ID. | SQL schema, procedure, permissions, backup, recovery, and outage evidence must come from the target or an explicitly authorized acceptance system. |
| Certificates | RSA signing certificates are resolved from the configured Windows certificate store. Signing requires an accessible private key; validation uses the public key. | Source and synthetic tests do not prove target store or runtime-identity access. |
| Health | `GET /api/v1/health` is anonymous and dependency-independent application liveness. | A `200 Healthy` response does not prove SQL, LDAP, DPAPI, certificate, or audit readiness. No readiness endpoint is assumed. |

## 2. Supported environment assumptions

### OS assumptions

- The supported hosting assumption is Windows Server hosting the ASP.NET Core application through IIS and the .NET 10 ASP.NET Core Hosting Bundle.
- The target must provide an IIS site, application pool, HTTPS binding, filesystem deployment location, and a Windows runtime identity selected by the environment owner.
- The runtime identity must be documented before acceptance. Its access to the application files, logs, certificate private key, DPAPI-protected directory secret, and SQL Server must be evidenced separately.
- No Linux, container, alternate web server, or non-IIS hosting path is accepted by this package because it is not the documented deployment model.

### Runtime assumptions

- The target runtime must satisfy the repository's `net10.0` target and the pinned .NET SDK/Hosting Bundle support requirements.
- The Release build and test evidence qualify repository behavior; they do not qualify an installed target runtime.
- Environment-specific `appsettings.json`, secret files, certificate references, and connection strings remain owned by the target environment. Repository defaults must not overwrite them.

### Database assumptions

- The database boundary is SQL Server using the checked-in Phase 11 schema and audit procedure contract.
- The application uses an encrypted SQL connection. Production certificate trust must be established through the host trust configuration; `TrustServerCertificate=True` is not an acceptance workaround.
- The target database must expose the required audit schema and `Audit.usp_WriteAuditEvent` procedure with the least permissions needed by the application identity.
- Retention, archival, purge, backup, restore, growth monitoring, and capacity ownership are operational decisions, not implemented application behavior.

### Directory assumptions

- The directory boundary is an authorized Active Directory-compatible LDAP service reachable over LDAPS. Production configuration must keep `UseLdaps=true` and port `636`.
- The configured domain, host, base DN, user search base DN, service-account identity, and group-to-role mappings must be supplied by the environment owner.
- The service-account password is a Windows DPAPI-protected file readable and decryptable by the approved runtime identity. Plaintext credentials must not be placed in configuration, evidence, logs, or source control.
- Acceptance does not require access to a production AD domain. A separately authorized representative directory or controlled acceptance directory may provide the evidence, provided its differences and limitations are recorded.

## 3. IIS acceptance checklist

Record the target identifier, date, operator, application version/commit, IIS site, application pool, and runtime identity for every item. A check without retained evidence is incomplete.

| Check | Required evidence | Result |
| --- | --- | --- |
| Hosting configuration | Approved site and app-pool names; HTTPS binding hostname/port; application path; in-process `AspNetCoreModuleV2` configuration; Hosting Bundle/runtime version; app-pool identity; no unapproved proxy or forwarding assumptions. | PASS / FAIL / NOT RUN |
| Application startup | Clean application start/recycle record; startup log showing successful configuration validation; no missing configuration, certificate, DPAPI, or dependency initialization error. Do not retain secrets. | PASS / FAIL / NOT RUN |
| Configuration loading | Sanitized effective-configuration review proving `AllowedHosts`, HTTPS issuer/audience, AD host/port/LDAPS mode, SQL encryption, certificate-store references, authorization mappings, and timeout/bound values. Redact passwords, connection credentials, and private key material. | PASS / FAIL / NOT RUN |
| Health and route behavior | HTTPS `GET /api/v1/health` returns the expected liveness response; an anonymous protected request returns the expected authentication challenge; host and certificate validation are performed on the real route. Do not interpret health as readiness. | PASS / FAIL / NOT RUN |
| Logging access | Approved operator can read the configured application logs and correlate startup/request failures. Evidence proves logs contain no password, token, raw authorization header, private key, or raw directory response. | PASS / FAIL / NOT RUN |
| Filesystem permissions | Runtime identity can read the deployed application and required non-secret configuration, write only approved log/temp locations, and cannot modify protected deployment or secret locations without authorization. | PASS / FAIL / NOT RUN |
| Application pool behavior | Start, stop, recycle, and failure behavior are recorded; the process returns to the expected state without destructive retries or manual secret exposure. | PASS / FAIL / NOT RUN |
| Transport limits | If the target route includes IIS or another proxy, execute the approved boundary probes for HTTPS, host selection, body/header limits, and supported JWT transport. TestServer results alone are insufficient. | PASS / FAIL / NOT RUN |

IIS acceptance is not complete merely because the site responds. The evidence must tie the responding process to the intended package, configuration ownership, runtime identity, HTTPS binding, and log location.

## 4. SQL acceptance checklist

SQL checks must use an explicitly authorized target. Do not use an implicit localhost fallback or an operational database for convenience. Do not include credentials, full connection strings, or sensitive audit data in the evidence package.

| Check | Required evidence | Result |
| --- | --- | --- |
| Connectivity | From the application runtime identity and target host, prove an encrypted SQL connection to the approved server/database using the environment-owned connection configuration. Record server/database identity, encryption state, and certificate-trust result without secrets. | PASS / FAIL / NOT RUN |
| Schema readiness | Verify the required database, schemas, tables, and expected audit columns exist at the approved version. Record the script/revision used and checksum or controlled change reference. No migration is performed by this phase. | PASS / FAIL / NOT RUN |
| Stored procedure | Execute an authorized non-sensitive audit write through `Audit.usp_WriteAuditEvent`; verify the returned audit ID and persisted fields match the request correlation ID and approved event. Remove or protect any resulting data under the retention policy. | PASS / FAIL / NOT RUN |
| Permissions | Prove the application identity has only the approved database/schema/procedure permissions required for audit writes and cannot perform unapproved administrative actions. Record the reviewer and permission source. | PASS / FAIL / NOT RUN |
| Backup ownership | Identify the SQL operational owner, backup schedule/policy, retention, restore authority, and evidence location. A backup claim is not accepted without a policy or execution record. | PASS / FAIL / NOT RUN |
| Recovery | Record an approved restore/recovery exercise or explicitly record that it remains an open gate. Include recovery owner and escalation path; do not perform production recovery as part of this phase. | PASS / FAIL / NOT RUN |
| Failure behavior | In a disposable or explicitly authorized non-production target, demonstrate unavailable SQL, timeout, cancellation, and recovery behavior. Record request result, audit outcome, elapsed time, logs, and correlation without secrets. Confirm the primary response is not silently represented as durable audit success. | PASS / FAIL / NOT RUN |
| Capacity | Record representative audit volume, storage baseline, growth threshold, and owner only after P6-D13 capacity inputs are approved. No threshold is invented here. | PASS / FAIL / NOT RUN |

The current application has no retry queue, replay path, retention job, purge automation, or database readiness check. These omissions must remain visible in the handoff and must not be converted into implied acceptance.

## 5. Active Directory / LDAP acceptance checklist

This checklist defines evidence for the directory boundary without requiring production AD access. Use a controlled acceptance directory or representative authorized environment when production access is not approved. Record its identity and limitations.

| Check | Required evidence | Result |
| --- | --- | --- |
| Connectivity and TLS | From the runtime identity, prove DNS resolution and LDAPS connectivity to the approved host on TCP 636; verify the server certificate chain, hostname identity, validity, and trust. Record protocol/endpoint metadata, never credentials or directory dumps. | PASS / FAIL / NOT RUN |
| Root DSE | Run the explicitly opted-in Root DSE acceptance check with approved protected inputs; retain a sanitized success/failure result and timestamp. This proves connectivity and bind/search behavior only. | PASS / FAIL / NOT RUN |
| Service-account secret | Prove the DPAPI-protected password file exists at the approved path, is readable/decryptable by the runtime identity, and is inaccessible to unauthorized identities. Do not reveal or copy the secret. | PASS / FAIL / NOT RUN |
| Account lookup | With a controlled test identity, prove configured-domain UPN lookup behavior, successful user bind, and safe handling of missing/invalid credentials. Do not require or record a production account. | PASS / FAIL / NOT RUN |
| Group lookup and mapping | With approved representative membership, prove complete group lookup, deterministic group-to-role mapping, and the protected endpoint authorization result. Record only sanitized role/test identifiers. | PASS / FAIL / NOT RUN |
| Permission requirements | Identify the directory permissions granted to the service account for user/group lookup and confirm no write or administrative permissions are required. | PASS / FAIL / NOT RUN |
| Failure scenarios | Evidence must cover unreachable host, TLS trust failure, service-secret failure, service bind failure, user bind failure, missing user, incomplete/invalid group data, timeout, cancellation, and directory outage. Confirm safe status/log behavior and no token issuance after failed directory stages. | PASS / FAIL / NOT RUN |
| Capacity inputs | Record representative concurrent login volume, directory limits, timeout expectations, and escalation owner only after P6-D13 is decided. The per-instance application limits are not an enterprise directory capacity claim. | PASS / FAIL / NOT RUN |

The repository's default tests intentionally do not contact AD. A successful default test run is not LDAP environment acceptance, and a Root DSE result is not real-user authentication acceptance.

## 6. TLS and certificate acceptance

Certificate operations remain outside this phase. No certificate is generated, installed, renewed, exported, or replaced here.

| Requirement | Required evidence | Owner |
| --- | --- | --- |
| HTTPS server certificate | Approved certificate inventory, subject/SAN match to the IIS binding, valid chain and dates, EKU appropriate for server authentication, and successful client validation on the target route. | Certificate owner: **TBD** |
| SQL TLS certificate | Evidence that the SQL endpoint certificate is trusted by the application host, matches the connection endpoint, and succeeds with encryption and `TrustServerCertificate=False`. | SQL/certificate owner: **TBD** |
| LDAP TLS certificate | Evidence that the LDAPS server certificate is trusted and name-valid from the application host on TCP 636. | AD/certificate owner: **TBD** |
| JWT signing certificate | Approved certificate-store location/name/thumbprint reference; valid RSA certificate within the implemented 2048-4096-bit policy; private-key access test under the runtime identity; public validation evidence. Never retain private key material. | Certificate owner: **TBD** |
| Certificate ownership | Named issuer/renewal authority, custody boundary, renewal procedure, emergency replacement procedure, and escalation route. | Certificate owner: **TBD** |
| Expiration monitoring | Approved monitoring lead time, alert destination, responsible operator, and tested escalation. No expiration threshold is selected by this document. | Operations/certificate owner: **TBD** |
| Trust management | Evidence of the approved Windows trust stores or chain policy and change authority. Do not use disabled validation or an accept-all callback. | Platform/security owner: **TBD** |

Certificate acceptance is incomplete when a certificate merely exists. The target must prove name, chain, validity, intended use, runtime identity access where required, and accountable renewal ownership.

## 7. Operational handoff

The acceptance record must include the following without inventing names:

| Handoff item | Required content | Current status |
| --- | --- | --- |
| Target environment owner | Environment name/identifier, owner, approval date, and scope of authority. | **TBD; P6-D12** |
| SQL operational owner | Database owner, backup/restore owner, permission approver, retention/growth authority, and escalation route. | **TBD; P6-D9/P6-D13** |
| AD owner | Directory service owner, service-account owner, permission approver, and outage escalation route. | **TBD; P6-D12/P6-D13** |
| Certificate owner | HTTPS, SQL TLS, LDAPS, and JWT signing certificate custody and renewal contacts. | **TBD** |
| Capacity thresholds | Approved IIS, API, LDAP, SQL, storage, audit latency, and outage thresholds with alert owners. | **TBD; P6-D13** |
| Runbooks | Startup/recycle, log access, configuration change, secret rotation, certificate renewal, SQL outage, LDAP outage, rollback, and escalation procedures. | **TBD; owner review required** |
| Evidence storage | Approved access-controlled location, retention period, naming/indexing convention, reviewer, and integrity/version record for acceptance evidence. | **TBD** |
| Handoff review | Operations, platform, DBA, AD, security, and application representatives confirm the evidence and open risks. | **NOT COMPLETE** |

### Acceptance decision record

The final record must state one of:

- `ACCEPTED` only when every mandatory checklist item has passing evidence, all required owners are named, approved capacity/operational decisions are recorded, and open exceptions are explicitly approved.
- `CONDITIONALLY ACCEPTED` only when an authorized owner records each exception, risk, expiry date, compensating control, and follow-up owner.
- `NOT ACCEPTED` when evidence is missing, contradictory, stale, or dependent on an unapproved environment change.

No status in this repository may claim `ACCEPTED` for a target environment without that external evidence package.

## Decision tracking

The following decisions remain open and must not be replaced with invented names or thresholds:

- Target environment owner: **TBD** (P6-D12).
- SQL operational owner: **TBD** (P6-D9 and P6-D13).
- AD owner: **TBD** (P6-D12 and P6-D13).
- Certificate owner: **TBD**.
- Capacity thresholds and representative load: **TBD** (P6-D13).
- Liveness/readiness semantics and any dependency-gated readiness contract: **TBD** (P6-D11).
- Audit loss, latency, retention, backup/recovery, and monitoring policy: **TBD** (P6-D7 through P6-D10).

## Validation and evidence boundaries

The repository validation for this documentation package is:

```powershell
dotnet restore
dotnet build LabAuthServer.slnx -c Release --no-restore
dotnet test LabAuthServer.slnx --no-build --no-restore
```

Those commands validate repository behavior only. They do not perform target acceptance, deployment, IIS installation, AD/domain change, SQL production change, certificate operation, release creation, or customer onboarding. No target environment is accepted by their result.

## Non-authorizations

This package does not authorize:

- production deployment or application replacement;
- IIS installation, site/app-pool creation, or IIS setting changes;
- AD/domain, service-account, or directory permission changes;
- SQL production schema, procedure, permission, backup, retention, or migration changes;
- certificate generation, installation, export, renewal, or replacement;
- release creation, tag/signing operation, artifact publication, or customer onboarding;
- new runtime behavior, readiness endpoint, retry queue, retention job, or monitoring integration;
- any Phase 5.6 modification.# Phase 6.6 — Target-Environment Acceptance

Status: PLANNED — NOT AUTHORIZED FOR IMPLEMENTATION.

## Purpose

Define a reproducible acceptance plan for the actual intended Windows/IIS/AD/SQL environment without performing environment changes in this planning task. This subphase is the explicit gate for environment readiness before any first-release authorization request.

## Scope of future acceptance

### Host/runtime

The plan must verify:

- target Windows Server version and patch state;
- IIS configuration and hosting bundle state;
- exact build/package identity;
- runtime identity and service account permissions.

### TLS and HTTP security

The plan must verify:

- HTTPS binding and certificate trust;
- certificate chain validation and SAN matching;
- HSTS and security-header behavior;
- host filtering and forwarded-header trust boundaries if applicable.

### JWT signing and key custody

The plan must verify:

- certificate availability in the chosen store;
- private-key accessibility to the application identity;
- active key selection and previous-key overlap behavior;
- consistent startup and issuance behavior.

### DPAPI and config protection

The plan must verify:

- file location and ACLs;
- application identity access to protected config or secret files;
- recovery expectations and lock-out conditions.

### LDAP / AD acceptance

With a controlled acceptance identity, the future run must verify:

- successful bind;
- group retrieval;
- role mapping;
- JWT issuance;
- protected endpoint access;
- known failure behavior.

### SQL acceptance

The plan must verify under the intended application identity:

- required permissions;
- stored procedure execution;
- denied unauthorized table access where applicable;
- audit persistence under the application identity.

The SQL permission analysis must include [database/Phase11/04_ConfigureAuditPermissions.sql](../../../database/Phase11/04_ConfigureAuditPermissions.sql).

## Failure scenarios to plan for

- AD unavailable;
- SQL unavailable;
- invalid/missing signing certificate;
- invalid/missing license;
- expired license;
- malformed configuration.

## Rollback and capacity evidence

This plan must include:

- rollback evidence required before commercial deployment;
- representative, bounded acceptance scenarios for authentication traffic, LDAP contention, audit latency, and dependency outage behavior;
- remaining decision gates for capacity thresholds if owner-defined numbers are required.

## Dependencies on earlier work

- Phase 6.2 and 6.3 must have corrected the technical boundaries to the extent required by the acceptance environment.
- Phase 6.4 must have reconciled the release build and documentation state.
- Phase 6.5 must have adopted the operational policy decisions for audit loss, latency, retention, and monitoring.

## External / operational / platform dependencies

- Windows Server and IIS access;
- AD/LDAP credentials and a controlled acceptance identity;
- SQL Server access and permission review;
- certificate and key custody review;
- DPAPI-protected file access under the target runtime identity;
- operations and DBA approval for the exact environment and evidence collection.

## Narrow implementation scope

This subphase is acceptance planning only. It does not change runtime configuration, install software, or make environment changes.

## Explicit non-goals

- no production deployment;
- no service rollout;
- no production certificate or license issuance;
- no database migration beyond the acceptance validation process;
- no change to runtime configuration in this planning task.

## Expected files/components

Likely validation inputs include:

- [database/Phase11/04_ConfigureAuditPermissions.sql](../../../database/Phase11/04_ConfigureAuditPermissions.sql)
- [docs/Deployment.md](../../Deployment.md)
- [docs/Operations.md](../../Operations.md)
- [docs/Authentication.md](../../Authentication.md)
- [docs/Authorization.md](../../Authorization.md)
- [docs/JWT.md](../../JWT.md)
- [docs/Database.md](../../Database.md)
- [docs/Security.md](../../Security.md)

## Required automated validation

The future acceptance run must include:

- environment acceptance matrix execution;
- certificate and key access validation under the exact app identity;
- AD authentication and token issuance validation with a controlled identity;
- SQL audit persistence validation under the app identity;
- failure scenario validation;
- rollback evidence capture and capacity evidence review.

## Acceptance criteria

The future acceptance is successful only if:

- the build identity and deployment package are correct;
- HTTPS/TLS and trust behavior are verified;
- JWT signing and prior-key overlap behavior is proven;
- AD/LDAP and SQL dependencies are proven under the production-like identity;
- failure scenarios are observed and recorded;
- rollback evidence exists before first commercial deployment.

## Implementation Authorization Packet

### Baseline prerequisites

- Phase 6.2–6.5 baseline decisions are approved and documented.
- The target Windows/IIS/AD/SQL environment is specified and authorized.
- The acceptance identity and permissions have been provisioned.

### Exact implementation scope

- target-environment acceptance matrix execution;
- runtime, TLS, JWT, DPAPI, LDAP, SQL, and failure-case checks;
- rollback evidence collection and capacity review.

### Explicit non-goals

- deployment or rollout;
- environment modification;
- live customer data or customer-facing issue.

### Expected files/components

- acceptance checklist and environment-specific evidence;
- SQL permission review and validation notes;
- rollback and capacity evidence pack.

### Tests/validation

- environment acceptance test matrix;
- AD bind and group mapping validation;
- SQL audit persistence and permission validation;
- failure-case validation and rollback evidence review.

### Acceptance criteria

- all target-environment checks produce evidence;
- no unchecked dependency remains open;
- rollback evidence is established before first release-related deployment.

### Owner/architect decisions required first

- exact target environment and operating identity;
- representative capacity thresholds if not already set by a formal owner decision;
- approval of rollback evidence standard.

### External dependencies

- target-host and IIS access;
- AD and SQL admin support;
- certificate and DPAPI owner input;
- operations/DBA evidence collection.

### Safety boundaries

- no environment change is performed during planning;
- no release authorization claim is generated by acceptance planning alone;
- no production signing material or customer data are used in this plan.

### Recommended signed commit message

Plan remaining Phase 6 work

---

This subphase remains planning-only and does not authorize implementation.
