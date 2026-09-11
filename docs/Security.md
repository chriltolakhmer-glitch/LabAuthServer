# Security

## Licensing (cross-reference)

LabAuthServer also carries an offline, signed license document that is verified with a trusted vendor public key set. Licensing never disables or weakens authentication, authorization, TLS, LDAP transport security, request limits or audit (D-15). Missing, invalid, unsupported or expired licenses place the application in Community/restricted mode; the application does not refuse to start. See [Licensing](Licensing.md) for the implemented behaviour, the source-available limitation and the deferred/future items.

## Implemented controls

- LDAPS-only directory communication over TCP 636.
- LDAP filter escaping and configured-domain UPN validation.
- Windows DPAPI-backed service-account credential loading.
- RSA certificate-store signing with private-key-only issuance and public-key-only validation.
- Certificate validity, RSA key-size, key-usage, EKU, and duplicate-match checks.
- Default authenticated-user policy and explicit role policies.
- Bounded fixed-window rate limiting on the login endpoint.
- Encrypted SQL transport by default; development trust exceptions are isolated to development/test configuration.
- Correlation IDs, minimized audit events, and generic ProblemDetails responses.
- Typed stored-procedure audit writes with sensitive JSON rejection.
- Startup validation for Active Directory, token, authorization, and audit configuration.
- An 8192-byte bounded login request body enforced before model binding.
- A 16128-byte aggregate decoded request envelope enforced before authentication.

## Implemented Phase 2A size boundaries

RSA keys must be within the fixed inclusive 2048–4096-bit range for active/previous signing and public validation. Existing validity, KeyUsage, no-EKU, duplicate-selection and private-key/public-key separation checks remain intact. Missing or expired previous-key overlap fails closed, including the alternate runtime provider's signing path.

The deployed reduced `MaximumTokenSize = 7680` issuance payload budget is separate from the fixed API `MaximumEncodedJwtSize = 12288` and 12352-byte Authorization-header value ceiling. Existing UTF-16 issuer/audience/subject checks and UTF-8 role/scope checks are unchanged. The worst supported issuer output is 11997 encoded bytes; see the [Phase 2A derivation](plans/Phase-2/Phase-2A-API-Resource-Protection.md#jwt-size-boundary-decision).

Login request bodies are read into a bounded 8193-byte buffer and reject at 8193 bytes with 413. The general policy counts the UTF-8 request target plus each decoded header name, field separators, value, and duplicate-value comma separator, rejecting above 16128 bytes with 431. It includes duplicate Authorization values once; `AuthorizationSizeMiddleware` remains the owner of the separate 12352-byte Authorization-value boundary. These application controls do not alter IIS or HTTP.sys machine-wide limits, and native rejections remain outside application observability.

The completed Authorization size middleware runs after the login limiter and before authentication: oversized values return 431, and oversized Bearer tokens inside the header budget return 401. The new general-header and endpoint-specific login-body middleware run immediately after routing and before the login limiter. Correlation and safe exception handling remain upstream. These limits bound application input after host parsing; they do not establish IIS/proxy capacity or protection against all resource exhaustion.

## Protected data boundaries

Passwords, bearer tokens, authorization headers, private keys, DPAPI contents, raw LDAP responses, and stack traces must not be placed in source control, logs, SQL audit details, or API responses. Use `<SECRET_FILE>`, `<THUMBPRINT>`, `<CONNECTION_STRING>`, and `<USERNAME>` placeholders in examples.

The repository does not implement MFA, federation, refresh tokens, stateful revocation, account lockout, or audit retention automation. Login rate limiting is an application-level control and does not replace AD account-lockout policy.

## Operational responsibilities

The target Windows identity must have only the access required to read the protected DPAPI file, use the signing certificate private key, and execute the audit writer procedure. Environment-specific identity and permission assignments belong in an approved private runbook, not public documentation.

## Verification boundary

Automated tests cover security behavior and sensitive-data rejection. They do not prove production certificate, DPAPI, AD, SQL identity, or IIS configuration. See [Validation Status](Validation_Status.md) for the current evidence boundary.

## 2026-09-08 reduction and deployment boundary

Option A reduces issuance and acceptance together. The prior 24576/24640-byte envelope was unreachable through the inspected native path. Actual HTTP/1.1 and HTTP/2 probes reached the new candidate boundaries, including one byte over, with normal headers and 3072 bytes of additional metadata. Those probes used the old deployment, so new-policy rejection after deployment remains pending. Other client profiles and proxy/LB capacity remain unverified. See [the measured budget decision and deployment plan](plans/Phase-2/JWT-Transport-Budget-Reduction.md). No deployment or native/TLS/key changes were made.

## Security Slice 2: host and response policy (source only)

The sole production hostname allowlist is `DC01.lab.local`; the existing outer host filter rejects unapproved/empty hosts before normal authentication/business work. `ApiResponseHeadersMiddleware` applies one `Cache-Control: no-store`, `X-Content-Type-Options: nosniff` and `X-Frame-Options: DENY` value to `/api` responses, including login, protected resources, health, early 413/431/429 and application errors. Health deliberately avoids stale liveness results; unrelated public paths retain their own policy. Status, body, content type, correlation and challenges are preserved. Native/outer host rejection is outside this policy.

DENY is justified by the absence of an embedding contract or HTML UI. CSP, Referrer-Policy and Permissions-Policy are deferred pending a meaningful browser document/feature surface. HSTS is explicitly not enabled: defer until hostname and ingress/proxy inventory, client trust, certificate continuity, subdomain scope, cached rollback recovery and client monitoring are assessed. No forwarding trust is configured. See [the full assessment, limitations and verification](plans/Phase-2/Phase-2A-Security-Slice-2.md). These changes are source-tested and NOT DEPLOYED; prior size controls are completed and deployed.

## Phase 2A LDAP failure classification (2026-09-10)

Service-bind LDAP 49 now means DirectoryUnavailable/503 with a ServiceBindRejected reason; only user rejection remains InvalidCredentials/401. Identity/group results must be complete and unambiguous. Null/zero/multiple identity results, malformed required identity/account-control data, partial membership and invalid group DNs fail closed as ProtocolFailure/503. Successful empty group membership remains distinct and retains the existing no-approved-role policy.

Directory failure messages are fixed and safe, with correlation when available. Expected failure logs and audit details contain bounded category/stage/reason and explicitly sourced numeric diagnostics, without raw exceptions, directory messages, DNs, filters or secrets. Existing audit codes are preserved; login success is audited only after token issuance. Cancellation/deadline and audit-persistence guarantees are not redesigned. See [classification, compatibility limits and tests](plans/Phase-2/Phase-2A-LDAP-Failure-Classification.md).
