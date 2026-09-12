# Phase 6.6 — Target-Environment Acceptance

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
