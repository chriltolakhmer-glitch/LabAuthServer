# Operations

## Pre-deployment requirements

Confirm that the target environment provides:

- .NET 10 ASP.NET Core Hosting Bundle.
- IIS site and application pool configuration.
- An approved HTTPS certificate and accessible RSA private key.
- LDAPS connectivity on TCP 636 with normal certificate validation.
- A readable Windows DPAPI-protected service-account secret file at `<SECRET_FILE>`.
- SQL connectivity through an approved `<CONNECTION_STRING>` and least-privilege application identity.

## Smoke checks

After an authorized deployment:

- `GET /api/v1/health` returns `200`.
- HTTP requests redirect to HTTPS where configured.
- Anonymous `GET /api/v1/protected` returns `401`.
- Correlation headers are canonical and returned on responses.
- Approved audit events can be written through the stored procedure.

A valid real-user login should be tested only through the protected operational process. Do not place the identity or credential in source control, documentation, logs, or issue reports.

## Troubleshooting order

1. Check application startup and configuration-validation logs.
2. Check HTTPS binding and certificate-store access.
3. Check DPAPI file existence and runtime identity permissions without exposing its contents.
4. Check LDAPS reachability, port 636, domain/UPN alignment, and search base.
5. Check SQL connectivity and procedure-execution permissions.
6. Check correlation IDs and safe audit-persistence diagnostics.

## Rollback

Use the approved deployment workflow to restore the last validated package. Database rollback must be handled by authorized database change control; do not delete audit history as an application rollback step.

Retention, archival, purge, and SQL Agent scheduling are not implemented by this repository.
