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

## JWT/header boundary operations

Application limit: **12352-byte Authorization-header value**, including the scheme and whitespace. Complete encoded JWTs are limited to **12288 bytes**. `MaximumTokenSize = 7680` remains the issuance payload budget; RSA policy is fixed at **2048–4096 bits** for active and previous keys.

External transport compatibility: **requires deployment verification**. Inventory IIS/native and all upstream hops, their effective field/aggregate limits and HTTP versions. The inspected in-process IIS deployment is not governed by Kestrel limits. Full HTTP/1 field framing adds 17 bytes to the value; other headers require additional aggregate headroom. See [Deployment](Deployment.md) before approving host changes.

Application size rejection returns an empty 431 for oversized Authorization values or an empty 401 with a Bearer challenge for oversized tokens within the header allowance. It retains correlation but deliberately avoids per-rejection authentication/audit work. An earlier HTTPS redirect, login 429 or native/proxy rejection can take precedence. Distinguish these outcomes using status and safe correlation metadata; never log the header or token. Valid requests continue through the existing authentication/audit pipeline.

## Troubleshooting order

1. Check application startup and configuration-validation logs.
2. Check HTTPS binding and certificate-store access.
3. Check DPAPI file existence and runtime identity permissions without exposing its contents.
4. Check LDAPS reachability, port 636, domain/UPN alignment, and search base.
5. Check SQL connectivity and procedure-execution permissions.
6. Check correlation IDs and safe audit-persistence diagnostics.

## Rollback

Use the approved deployment workflow to restore the last validated package. Database rollback must be handled by authorized database change control; do not delete audit history as an application rollback step.

For this increment, preserve the current package/configuration and active/overlap certificate inventory before rollout. If compatibility fails, restore the validated package and its matching configuration, recycle the approved app pool, then repeat health, HTTPS, anonymous/authorized protected access and login-limiter checks. No schema migration is involved; restore the matching `Token:MaximumTokenSize` with the preserved package. A pre-boundary package removes these new size/RSA-maximum controls: review that exposure and retain approved finite host protection. Restore any separately approved host change through its own rollback procedure; never restore private-key-dependent bearer validation or silently remove an overlap key.

Retention, archival, purge, and SQL Agent scheduling are not implemented by this repository.

## 2026-09-08 reduction and deployment boundary

Option A reduces issuance and acceptance together. The prior 24576/24640-byte envelope was unreachable through the inspected native path. Actual HTTP/1.1 and HTTP/2 probes reached the new candidate boundaries, including one byte over, with normal headers and 3072 bytes of additional metadata. Those probes used the old deployment, so new-policy rejection after deployment remains pending. Other client profiles and proxy/LB capacity remain unverified. See [the measured budget decision and deployment plan](plans/Phase-2/JWT-Transport-Budget-Reduction.md). No deployment or native/TLS/key changes were made.
