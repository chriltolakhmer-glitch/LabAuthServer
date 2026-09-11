# Configuration

Configuration is bound and validated at startup. Public documentation should describe setting names, not deployment values.

## Licensing

The licensing subsystem reads a single configuration value under the `Licensing` section:

- `LicenseFilePath`: configured path to the signed license file. The default is empty, which means no license is configured. No default path is hard-coded. A missing configuration yields Community/restricted mode rather than an exception.

The server currently has no runtime license-file loader and no `Licensing` section in the shipped `appsettings*.json`. The concrete production license file location remains an open decision (see [Phase 4 Decision Log](plans/Phase-4/Phase-4-Decision-Log.md), O-10). See [Licensing](Licensing.md).

## ActiveDirectory

- `Domain`: configured UPN suffix, for example `<DOMAIN>`.
- `Host`: LDAP server, for example `<LDAP_HOST>`.
- `Port`: `636` when LDAPS is enabled.
- `BaseDn`: directory base distinguished name.
- `UserSearchBaseDn`: user/group search base.
- `ServiceAccountUsername`: non-secret directory-query identity.
- `ServiceAccountPasswordFile`: absolute `<SECRET_FILE>` path to the Windows DPAPI-protected password.
- `UseLdaps`: must be enabled in Production.
- `ConnectionTimeout`: positive LDAP connection timeout; not a total authentication deadline.
- `AuthenticationTimeout`: application decision budget shared across identity, groups, mapping and token issuance. Default/source example `00:00:30`; startup validation requires 1 through 60 seconds inclusive. Invalid configured values fail validation. Native synchronous work can finish after this decision deadline; late results are rejected and cleanup remains awaited. See [cooperative semantics](plans/Phase-2/Phase-2A-LDAP-Cancellation-Deadlines.md). Live/environment-specific settings are preserved.

- `MaxConcurrentLdapOperations`: default/source example `4`; startup validation requires an integer from `1` through `32`. Bounds the public login LDAP phase per application instance, including identity/groups/cleanup, not enterprise-wide directory capacity. The singleton limit is fixed for its service-provider lifetime. Waiting consumes the existing AuthenticationTimeout budget. Live/environment-specific settings remain untouched. See [concurrency scope and waiter limits](plans/Phase-2/Phase-2A-LDAP-Concurrency-Resource-Protection.md).

The approved `ActiveDirectory:MaxPendingLdapWaiters` defaults to `16` and validates an integer from `1` through `128` at startup and gate construction. It bounds incomplete LDAP permit waits independently from active concurrency, per application instance. Full pending capacity returns the typed ResourceExhausted/503 outcome before LDAP work, with a generic message and bounded audit. Caller cancellation remains 499, deadline/equality 504. No unlimited value or live configuration change. See [admission, cleanup and evidence](plans/Phase-2/Phase-2A-Hard-Pending-Waiter-Cap.md).

## Token

- `Issuer`: absolute HTTPS URI, `<ISSUER_HTTPS_URI>`.
- `Audience`: configured API audience, `<AUDIENCE>`.
- `AccessTokenLifetime`: at least one second and no greater than one day, matching the claims builder.
- `ClockSkew`: zero to five minutes.
- `SigningAlgorithm`: exactly `RS256`, `RS384`, `RS512`, `PS256`, `PS384` or `PS512`; supplied configuration uses `RS256`. Unsupported RS/PS-prefixed names fail startup.
- `ActiveKeyId`, optional previous key metadata, and approved key identifiers.
- Previous overlap expiry must be in the future at startup. During an already-started process, expiry retires only the previous key; runtime structural validation preserves active signing/validation. A new startup still requires stale previous metadata to be cleaned up. See [final code review](plans/Phase-2/Phase-2A-Final-Application-Code-Review.md).
- Certificate store location/name and `<THUMBPRINT>` values.
- `SigningKeyStoreReference`: reference only; it must not contain private key material.
- `MaximumClaimSize = 4096` and `MaximumTokenSize = 7680` in the supplied configuration. The latter bounds serialized issuance payload bytes and the existing aggregate claim estimate, not complete encoded JWTs. Issuer/audience/subject limits count UTF-16 code units; role/scope limits count UTF-8 bytes. These semantics are unchanged.

Certificate policy requires a currently valid RSA certificate within the fixed inclusive 2048–4096-bit range. Signing requires a private key; validation uses only the public key. Incompatible KeyUsage is rejected, certificates containing an EKU extension are rejected under the current general-purpose signing policy, and multiple currently valid matching certificates fail closed. The range applies to active and previous keys and is not operator-configurable.

## Fixed JWT/HTTP transport policy

`JwtRequestSizePolicy` defines `MaximumEncodedJwtSize = 12288` bytes and `MaximumAuthorizationHeaderSize = 12352` bytes for the Authorization value, including `Bearer `. These are code constants, not `Token` or `HttpSecurity` configuration keys. Change only `Token:MaximumTokenSize` to 7680 in the preserved deployment settings when rollout is authorized; never replace live settings wholesale with source defaults.

The default payload yields at most 11997 encoded bytes under the supported RSA/header envelope, leaving a 291-byte token margin. API startup validates `1072 + ceil(4 * MaximumTokenSize / 3) + 683 + 2 <= 12288`; inconsistent payload overrides fail startup. Serializer, algorithm or header changes require a fresh size review. See [JWT](JWT.md) for derivation and rejection behavior. IIS/proxy field and aggregate capacity requires deployment verification; the application policy does not configure those hosts.

## Authorization

- `DefaultDeny`: must be `true`.
- `MaximumGroupCount`: positive bounded group limit.
- `PublicEndpoints`: explicit health/login allowlist.
- `GroupToRoleMappings`: validated approved group-to-role entries.

## Audit

- `ConnectionString`: deployment-specific `<CONNECTION_STRING>` with `Encrypt=True`. Production SQL Server certificate trust must be established through the host trust configuration; do not use `TrustServerCertificate=True` as a production bypass.
- `CommandTimeoutSeconds`: positive timeout no greater than 60 seconds.

The checked-in development/test connection may use `Encrypt=True;TrustServerCertificate=True` only when the local SQL Server does not provide a trusted certificate. This exception is isolated to development/test configuration and must not be copied to production.

## Login rate limiting

`POST /api/v1/auth/login` uses one bounded fixed-window policy: 10 requests per minute, no queue. The policy intentionally uses one constant partition so attacker-controlled usernames or addresses cannot create an unbounded in-memory partition set. Health and protected endpoints are not rate limited by this policy.

Sensitive values must be supplied through approved host configuration and protected operating-system resources. Do not commit passwords, private keys, tokens, or DPAPI contents.

## 2026-09-08 reduction and deployment boundary

Option A reduces issuance and acceptance together. The prior 24576/24640-byte envelope was unreachable through the inspected native path. Actual HTTP/1.1 and HTTP/2 probes reached the new candidate boundaries, including one byte over, with normal headers and 3072 bytes of additional metadata. Those probes used the old deployment, so new-policy rejection after deployment remains pending. Other client profiles and proxy/LB capacity remain unverified. See [the measured budget decision and deployment plan](plans/Phase-2/JWT-Transport-Budget-Reduction.md). No deployment or native/TLS/key changes were made.

## Security Slice 2 source configuration (2026-09-09)

Source `AllowedHosts` is now `DC01.lab.local`, replacing `*`, based on the inspected HTTP/HTTPS IIS bindings. No IP, localhost or alias is included. ASP.NET Core's existing host filter is retained with `AllowEmptyHosts = false`; matching is case-insensitive and ignores numeric ports. This is source-only: Current still has its original settings. During separately authorized rollout, preserve all environment-specific SQL, AD and signing settings; never deploy repository appsettings wholesale. Check environment overrides and probe Host values before rollout. Local development hostname requirements need an explicit development-only configuration.

`ApiResponseHeadersMiddleware` owns the fixed `/api` response headers; there are no new header configuration keys. HSTS and proxy trust configuration are unchanged. See [the policy and deployment boundary](plans/Phase-2/Phase-2A-Security-Slice-2.md). The earlier September 8 pre-deployment narrative above is historical: JWT/Authorization and body/aggregate-header controls are now deployed and accepted.
