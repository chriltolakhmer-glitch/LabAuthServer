# Troubleshooting Guide

## LDAPS authentication failure

Symptoms:

- login fails with `401` or service-unavailable behavior
- the application cannot bind to the domain controller

Likely causes:

- invalid or stale service-account password file
- incorrect `ActiveDirectory:Host` or `BaseDn`
- LDAPS disabled or port 636 not in use
- mismatched user UPN domain
- AD service account not available or disabled

Remediation:

- verify the DPAPI-backed file still exists and can be decrypted by the runtime identity
- confirm `Host` is `DC01.lab.local` and port is `636`
- confirm LDAP is enabled and the connection uses `UseLdaps = true`
- verify the supplied login username is a valid UPN in the configured domain

## Stale service-account DPAPI credential

This is a real historical issue. The app fail-closes when the protected file is missing, empty, unreadable, or cannot be decrypted. In that case the login flow does not fall back to anonymous bind; it ends in a safe authentication failure.

Checklist:

- confirm `C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi` exists
- confirm the runtime identity can read the file
- confirm the file was not replaced with stale or malformed output
- re-encrypt the service account password through the approved Windows DPAPI process only

## Incorrect search base

Symptoms:

- group membership resolution fails or returns no group identifiers
- a valid user can authenticate but receives no role mapping

Checklist:

- confirm `UserSearchBaseDn` is `DC=lab,DC=local`
- confirm the target AD structure still matches the expected domain layout
- check for AD changes that moved or renamed relevant containers

## UPN bind versus DN bind

The implementation intentionally uses UPN bind. If the supplied username does not match the configured domain suffix, authentication fails before any further AD lookup. This is a documented safety measure and is not a hidden fallback.

## JWT authentication configuration registration defect

This was a real historical production defect: the bearer configuration was registered through a named-options implementation path that was not the effective production pipeline. The result was that the custom secure JWT validation settings were not applied in production, leaving framework defaults active instead of the intended validation behavior.

The fix was to register the JWT bearer configuration through the effective production options interface. The project documents this as part of the final validation history and does not reopen phase 17 behavior.

## IIS deployment partial-copy problem

This class of issue occurs when runtime files are copied without a staged validation and parity check. It can leave the site in a partial state or with a mismatched runtime set.

The safe deployment script prevents this by:

- staging the package first
- checking file parity and hash parity
- verifying the config is production-safe
- verifying the runtime file count before replacement
- rolling back if any validation fails

## Safe deployment behavior

The script does not modify source, the certificate store, or the DPAPI secret. It stages a release, validates the package, replaces `Current`, and immediately rolls back if validation fails. This materially reduces the risk of a partial or mismatched IIS deployment.

## Additional troubleshooting notes

- if `/api/v1/health` does not return `200`, verify the app pool and IIS site are running
- if `/api/v1/protected` returns `401` for anonymous requests, this is expected and indicates the default deny policy is working
- if an authorized Reader receives `403`, verify the token role and the applicable AD group mapping
- if SQL audit records are missing, confirm the application identity has the expected SQL permission and that the connection string is valid
