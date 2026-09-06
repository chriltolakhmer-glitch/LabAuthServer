# Authentication

## Flow

1. The client sends `POST /api/v1/auth/login` over HTTPS.
2. The username must be a UPN matching the configured `<DOMAIN>`.
3. The service binds with the supplied user credentials over LDAPv3/LDAPS on TCP 636.
4. After authentication, the service resolves the user’s AD groups through the configured search boundary.
5. Approved groups are mapped to one application role.
6. The token service issues an RSA-signed JWT and returns the token response.
7. The outcome is sent to the audit service when audit persistence is available.

Passwords are used for the bind operation only. They are not returned, logged, placed in JWT claims, or written to SQL audit records.

## LDAP and service account

The `ActiveDirectory` configuration contains `<DOMAIN>`, `<LDAP_HOST>`, base/search distinguished names, service-account username, an approved `<SECRET_FILE>` path, LDAPS settings, and a connection timeout. The service-account password is loaded by the Windows DPAPI LocalMachine provider. Missing, unreadable, empty, or undecryptable protected credentials fail closed; there is no anonymous-bind fallback.

Platform certificate validation remains enabled. The implementation rejects non-LDAPS authentication and non-636 configuration. LDAP filter values are escaped before group searches.

## Failure behavior

Invalid UPN or request input produces a safe client error. Invalid credentials produce `401`; directory unavailability produces `503`; timeouts produce `504`; unexpected/configuration failures produce `500`. Login requests sent over HTTP produce `400` before authentication is attempted. Responses do not expose LDAP exception details.

## Authorization handoff

A successful user bind does not by itself grant a token. The resolved groups must produce exactly one approved role through the configured mapping and precedence rules. If no approved role is available, token issuance fails closed.

See [Authorization](Authorization.md), [JWT](JWT.md), and [Validation Status](Validation_Status.md) for the current boundaries and evidence.
