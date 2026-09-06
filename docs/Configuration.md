# Configuration Reference

## Overview

The application configuration is split between application files, environment-specific values, and protected operating-system resources. The repository does not contain production secrets, private keys, or JWT material.

## Core configuration sections

### `ActiveDirectory`

- `Domain` — AD domain name
- `Host` — LDAP server host (`DC01.lab.local`)
- `Port` — 636 for LDAPS
- `BaseDn` — base distinguished name
- `UserSearchBaseDn` — user search path
- `ServiceAccountUsername` — AD service account for directory queries
- `ServiceAccountPasswordFile` — path to the DPAPI-protected credential file
- `UseLdaps` — true
- `ConnectionTimeout` — 10 seconds

### `Token`

- `Issuer` — HTTPS issuer URI
- `Audience` — expected API audience
- `AccessTokenLifetime` — JWT expiry window
- `ClockSkew` — validation tolerance
- `SigningAlgorithm` — `RS256`
- `ActiveKeyId` — active key identifier
- `PreviousKeyId` — previous key identifier if configured
- `ApprovedKeyIds` — list of approved key identifiers
- `SigningCertificateStoreLocation` — `LocalMachine`
- `SigningCertificateStoreName` — `My`
- `SigningCertificateThumbprint` — certificate thumbprint
- `SigningKeyStoreReference` — certificate reference for operational use
- `MaximumClaimSize` — configured maximum claim size
- `MaximumTokenSize` — configured maximum token size

### `Authorization`

- `DefaultDeny` — must be `true`
- `MaximumGroupCount` — limit for role evaluation
- `PublicEndpoints` — `/api/v1/health`, `/api/v1/auth/login`
- `GroupToRoleMappings` — approved group-to-role mappings

### `Audit`

- `ConnectionString` — SQL Server connection string for the application database
- `CommandTimeoutSeconds` — timeout of 5 seconds

## Secret storage and protection

| Secret type | Storage location | Protection | Access requirement |
| --- | --- | --- | --- |
| LDAP service-account password | `C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi` | Windows DPAPI, LocalMachine scope | service account runtime identity |
| Signing certificate private key | Windows certificate store | OS certificate protection | authorized service account |
| SQL connection details | server configuration and app settings only | Windows authentication and local configuration controls | server/app pool identity |

The repository never contains the secret value itself or any private key material.

## Configuration reference status

This is a safe configuration reference only. It documents the setting names, their purpose, and the protection boundaries without exposing any actual secret values.
