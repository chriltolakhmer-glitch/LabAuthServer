# Audit Logging

## Implementation

The API publishes approved authentication, authorization, security, validation, and exception events through `IAuditEventService`. `SqlAuditEventService` validates the event, binds typed SQL parameters, and calls `Audit.usp_WriteAuditEvent`. It does not construct dynamic SQL or write directly to audit tables.

## Event categories

The database reference catalog contains:

- `AUTH_LOGIN_SUCCESS`
- `AUTH_LOGIN_FAILURE`
- `AUTH_LDAP_FAILURE`
- `AUTHZ_ACCESS_GRANTED`
- `AUTHZ_ACCESS_DENIED`
- `SEC_INVALID_TOKEN`
- `SEC_EXPIRED_TOKEN`
- `SEC_INVALID_SIGNATURE`
- `SEC_INVALID_ISSUER`
- `SEC_INVALID_AUDIENCE`
- `SEC_INVALID_ROLE`
- `APP_UNHANDLED_EXCEPTION`
- `APP_VALIDATION_ERROR`

Events can contain bounded correlation/request identifiers, normalized username or subject, approved role, endpoint, HTTP method, status, outcome, client address, server/version metadata, and allowlisted JSON details. Passwords, tokens, authorization headers, private keys, DPAPI contents, raw LDAP responses, and exception dumps are prohibited.

## Correlation and middleware

`CorrelationMiddleware` accepts a canonical `X-Correlation-ID` when supplied or creates a new GUID. The response includes the canonical correlation header, and the value is passed into audit records. `AuthorizationAuditMiddleware` records post-policy `403` outcomes. JWT bearer failures are classified without persisting token contents. Global exception handling returns generic correlated ProblemDetails and records a minimized unhandled-exception event.

## Database boundary and failure behavior

The writer procedure validates event codes, roles, status codes, JSON, lengths, and prohibited sensitive properties before an atomic insert. The application identity is intended to receive only procedure execution through `LabAuthServer_AuditWriter`.

Audit persistence failure is logged as an operational warning/error and does not replace the primary response. Retention, archival, purge procedures, and SQL Agent scheduling are not implemented.
