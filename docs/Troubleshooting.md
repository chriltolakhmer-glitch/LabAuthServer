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

## Licensing

The operator-facing entry points for a licensing problem are the public-safe `LicenseValidationStatus` and the internal `LicenseValidationReason`. Do not paste the internal reason code into customer-facing output, and never request the vendor private signing key to diagnose a problem. Map the status to a cause (see [Licensing](Licensing.md)): `Valid`, `Missing`, `Malformed`, `UnsupportedVersion`, `WrongProduct`, `InvalidSignature`, `UnknownKey`, `Expired`, `NotYetValid`, `InvalidConfiguration`, `InvalidContent`. A `Missing`, `Expired` or otherwise invalid license places the application in Community/restricted mode; it does not refuse to start. If the cause is a wrong or invalid license, request a corrected license from the vendor instead of weakening a validation rule. Note: the server currently has no runtime license-file loader and no licensing audit surface, so this mapping is used through the validator boundary and the test suites rather than through a runtime diagnostic log.

## Deployment failure

Validate the release package before replacing the deployed directory. Confirm the IIS hosting bundle, app pool identity, HTTPS binding, external configuration, DPAPI access, certificate access, LDAPS connectivity, and SQL permission boundary.
