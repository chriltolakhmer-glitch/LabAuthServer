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

## Authorization

- `DefaultDeny`: must be `true`.
- `MaximumGroupCount`: positive bounded group limit.
- `PublicEndpoints`: explicit health/login allowlist.
- `GroupToRoleMappings`: validated approved group-to-role entries.

## Audit

- `ConnectionString`: deployment-specific `<CONNECTION_STRING>`.
- `CommandTimeoutSeconds`: positive timeout no greater than 60 seconds.

Sensitive values must be supplied through approved host configuration and protected operating-system resources. Do not commit passwords, private keys, tokens, or DPAPI contents.
