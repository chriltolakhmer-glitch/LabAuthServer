# Phase 11 Operations and Deployment Readiness

**Status:** Ready for separately authorized deployment; deployment not performed.  
**Date:** 2026-09-04

## Runtime Configuration

- SQL Server instance validated: default `MSSQLSERVER` at `localhost`.
- Database: `LabAuthServer`.
- Application connection uses Windows Authentication and contains no SQL username/password.
- Application identity: inherited IIS `ApplicationPoolIdentity`, mapped as `IIS APPPOOL\LabAuthServerAppPool`.
- Application role: `LabAuthServer_AuditWriter`.
- Granted permission: `EXECUTE` on `Audit.usp_WriteAuditEvent` only.

## Database Change Set

Apply in order through DBA-controlled change management:

1. `database/Phase11/01_CreateDatabase.sql`
2. `database/Phase11/02_CreateSchemasTables.sql`
3. `database/Phase11/03_CreateAuditProcedures.sql`
4. `database/Phase11/04_ConfigureAuditPermissions.sql`

Scripts are idempotent where practical. The scripts do not create a purge procedure or SQL Agent job because retention/archive ownership is not approved.

## Monitoring

Monitor application audit-persistence warnings, stored-procedure failures, SQL availability, audit write latency, event volume, and invalid-token event volume. Do not include SQL exception text, connection strings, credentials, bearer tokens, or request bodies in alerts.

## Rollback

Application rollback uses the existing release/deployment process and must not modify `Current` during this validation record. Database rollback is not an automatic destructive operation: preserve audit evidence, disable the application audit registration through an approved release/configuration change if necessary, and use DBA-reviewed corrective scripts. Do not drop the database, tables, or audit history as a routine rollback.

## Retention and Maintenance

The proposed 12-month online retention remains deferred. Archive destination, legal hold, purge ownership, immutability controls, maintenance identity, and SQL Agent schedule require explicit approval. No purge procedure, purge role, or SQL Agent job exists.

## Security Boundaries

IIS bindings and application-pool identity were not changed. The deployed `C:\Apps\LabAuthServer\Current` directory was not modified. DPAPI contents and certificate private keys were not accessed or exported. Deployment and IIS restart were not performed.

## Validation Record

- `dotnet restore`: passed.
- Release solution build: passed with zero errors.
- Unit tests: 99 passed, 0 failed, 0 skipped.
- Integration tests: 46 passed, 0 failed, 0 skipped.
- SQL writer: successful result with `AuditEventId`; malformed JSON rejected.
- Effective app identity permissions: writer `EXECUTE=1`; audit table `SELECT=0`, `INSERT=0`; purge `EXECUTE=0`.
- Security scan: no value-bearing credential/token/header logging or SQL passwords found.
- Local health: `200 {"status":"Healthy"}`.
