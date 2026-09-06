# Configuration

Configuration is bound and validated at startup. Public documentation should describe setting names, not deployment values.

## ActiveDirectory

- `Domain`: configured UPN suffix, for example `<DOMAIN>`.
- `Host`: LDAP server, for example `<LDAP_HOST>`.
- `Port`: `636` when LDAPS is enabled.
- `BaseDn`: directory base distinguished name.
- `UserSearchBaseDn`: user/group search base.
- `ServiceAccountUsername`: non-secret directory-query identity.
- `ServiceAccountPasswordFile`: absolute `<SECRET_FILE>` path to the Windows DPAPI-protected password.
- `UseLdaps`: must be enabled in Production.
- `ConnectionTimeout`: positive LDAP connection timeout.

## Token

- `Issuer`: absolute HTTPS URI, `<ISSUER_HTTPS_URI>`.
- `Audience`: configured API audience, `<AUDIENCE>`.
- `AccessTokenLifetime`: positive value no greater than one day.
- `ClockSkew`: zero to five minutes.
- `SigningAlgorithm`: approved RSA-compatible algorithm; supplied configuration uses `RS256`.
- `ActiveKeyId`, optional previous key metadata, and approved key identifiers.
- Certificate store location/name and `<THUMBPRINT>` values.
- `SigningKeyStoreReference`: reference only; it must not contain private key material.
- `MaximumClaimSize` and `MaximumTokenSize`.

Certificate policy requires a currently valid RSA certificate with a key of at least 2048 bits. Signing requires a private key; validation uses only the public key. Incompatible KeyUsage is rejected, certificates containing an EKU extension are rejected under the current general-purpose signing policy, and multiple currently valid matching certificates fail closed.

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
