# Phase 14 - Build and Validation

**Date:** 2026-09-04
**Status:** COMPLETE

## Restore and Release build

- `dotnet restore .\Source\LabAuthServer\LabAuthServer.slnx`: PASS.
- `dotnet build .\Source\LabAuthServer\LabAuthServer.slnx -c Release --no-restore --nologo`: PASS.
- Build result: zero errors and zero warnings.

## Tests

- Unit tests: 112 passed, 0 failed, 0 skipped.
- Integration tests: 51 passed, 0 failed, 0 skipped.
- Security regression tests: PASS.
- Full suite: 163 passed, 0 failed, 0 skipped.
- Repeat full suite: PASS with the same result.
- No flaky tests observed.

## Security scan

The final scoped scan of `src/`, `tests/`, and `docs/`, excluding `bin/` and `obj/`, found no actual credentials, passwords, JWTs, private keys, DPAPI contents, unsafe logging, direct audit-table writes, insecure SQL, or plaintext LDAP implementation. Matches were safe property names, policy values, documentation examples, or test placeholders. Package vulnerability detection found no vulnerable packages from the configured NuGet source.

## Health check

The isolated local development process returned:

- HTTP status: 200.
- Body: `{"status":"Healthy"}`.
- `X-Correlation-ID`: present and canonical.

The process was stopped after validation. IIS was not used.

## Protected-boundary verification

- IIS deployment: not performed.
- `C:\Apps\LabAuthServer\Current`: not modified.
- IIS configuration: not modified.
- AD/LDAP configuration: not modified.
- Certificate configuration: not modified.
- Certificate private keys: not exported or inspected.
- DPAPI secret contents: not accessed or exposed.
- SQL permissions: no additional permissions granted.

## Known limitations

Live AD user authentication, certificate-store private-key behavior, DPAPI decryption behavior, and app-pool identity switching remain boundary validations and were not expanded into destructive or secret-inspecting tests. Retention, archival, purge, and SQL Agent scheduling remain deferred under Phase 11 approval. SQL transport remains the approved local `localhost` configuration; remote encrypted transport requires a separate deployment decision.

## Final acceptance

**PASS.** Phase 13 security review and Phase 14 build/validation gates passed. Phases 15-18 are documented separately.
