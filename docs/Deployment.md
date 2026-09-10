# Deployment

## Deployment model

The API includes a file-system publish profile and is designed for Windows/IIS hosting with the .NET 10 ASP.NET Core Hosting Bundle. Deployment requires an approved HTTPS binding, external configuration, directory connectivity, certificate-store access, DPAPI access, and SQL permissions.

Use placeholders in environment-specific runbooks:

- `<IIS_SITE>`
- `<IIS_APP_POOL>`
- `<DEPLOYMENT_PATH>`
- `<HOST>`
- `<LDAP_HOST>`
- `<DB_SERVER>`
- `<SECRET_FILE>`
- `<THUMBPRINT>`

## Release flow

1. Restore and build the solution in Release mode.
2. Run the unit and integration suites.
3. Publish the API to a clean release directory.
4. Validate the package contents and hashes.
5. Stage the package and preserve the current deployment for rollback.
6. Replace the application only after staging validation passes.
7. Start the approved app pool and verify health and anonymous protected-resource behavior.

The safe deployment procedure is an operational workflow, not a source-code feature. Use the separately controlled deployment script and do not copy secret-bearing files into a release package.

## JWT and Authorization transport compatibility

Application limit: **12352-byte Authorization-header value**. The complete encoded JWT limit is **12288 bytes**; the issuance payload is reduced to **7680 bytes**. Active and overlap RSA keys must be within **2048–4096 bits**. Inventory both before rollout; an out-of-range key requires an approved migration, not a larger policy override.

External transport compatibility: **requires deployment verification**. The inspected local IIS site uses in-process hosting, so Kestrel settings do not control its native header parser. No IIS/native/proxy setting is changed by this implementation. Absence of explicit per-header request-filtering entries does not prove that native per-field or aggregate limits admit the application envelope.

Verify every deployed hop's effective per-field and aggregate limits and its HTTP-version accounting. A full HTTP/1 Authorization field at the value ceiling occupies 12369 bytes including the field name, separator and CRLF. Aggregate capacity must also allow legitimate Host, correlation and other headers. This calculation is not an instruction to assign a uniform IIS/registry value. Do not increase native limits for this increment; stop and reassess a smaller coordinated budget if transport rejects first.

In staging, send a supported maximum-size issued token and valid boundary fixtures over the actual HTTPS route; verify protected access and oversized rejection at each hop without recording credentials. Host rejection can precede application correlation and can use host-specific codes. TestServer evidence establishes application behavior only.

## Current validation boundary

The repository proves that the publish profile and deployment procedure exist. It does not independently prove that a current target IIS deployment, real AD identity, certificate private key, or DPAPI secret is available. See [Validation Status](Validation_Status.md).

Historical IIS and release evidence is preserved under `docs/archive/deployment-evidence/` and is not current deployment proof.

## 2026-09-08 reduction and deployment boundary

Option A reduces issuance and acceptance together. The prior 24576/24640-byte envelope was unreachable through the inspected native path. Actual HTTP/1.1 and HTTP/2 probes reached the new candidate boundaries, including one byte over, with normal headers and 3072 bytes of additional metadata. Those probes used the old deployment, so new-policy rejection after deployment remains pending. Other client profiles and proxy/LB capacity remain unverified. See [the measured budget decision and deployment plan](plans/Phase-2/JWT-Transport-Budget-Reduction.md). No deployment or native/TLS/key changes were made.
