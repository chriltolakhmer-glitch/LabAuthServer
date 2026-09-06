# Troubleshooting

## Startup failure

Configuration is validated on startup. Check the `ActiveDirectory`, `Token`, `Authorization`, and `Audit` sections for missing, malformed, or disallowed values. Do not copy secrets into logs or issue reports.

## Login failure

Check, in order:

- The request uses HTTPS.
- The username is a UPN matching `<DOMAIN>`.
- LDAPS is enabled and uses TCP 636.
- The directory certificate is trusted by the Windows platform.
- The protected `<SECRET_FILE>` exists and is readable/decryptable by the runtime identity.
- The configured search base and group mappings match the target directory.

The service does not fall back to anonymous bind. Invalid credentials are intentionally reported generically.

## Token failure

For `401` responses, check issuer, audience, expiration, algorithm, `kid`, required claims, singular approved role, and certificate-store public-key resolution. Do not log or paste bearer tokens.

For `403` from `/api/v1/protected`, the token authenticated successfully but did not satisfy the Reader policy.

## Audit failure

Check the deployment-specific SQL connection, command timeout, stored-procedure existence, event catalog, and application permission to execute the writer procedure. Audit persistence failures must not be repaired by granting broad table permissions.

## Deployment failure

Validate the release package before replacing the deployed directory. Confirm the IIS hosting bundle, app pool identity, HTTPS binding, external configuration, DPAPI access, certificate access, LDAPS connectivity, and SQL permission boundary.
