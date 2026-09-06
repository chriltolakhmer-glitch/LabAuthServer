# Testing

## Toolchain

- .NET SDK: `10.0.400`, pinned by `global.json`.
- Target framework: `net10.0`.
- Test framework: xUnit with the .NET test SDK.
- Integration host: `Microsoft.AspNetCore.Mvc.Testing`.

## Projects

- `tests/LabAuthServer.UnitTests` covers application and infrastructure seams, configuration validation, LDAP boundaries, token issuance/signing/validation, role mapping, audit validation, and middleware behavior.
- `tests/LabAuthServer.IntegrationTests` covers API/controller behavior, the ASP.NET Core request pipeline, protected-resource authorization, health behavior, authentication contracts, and selected environment/database boundaries.

## Commands

Run from the repository root:

```powershell
dotnet restore .\LabAuthServer.slnx
dotnet build .\LabAuthServer.slnx -c Release --no-restore --nologo
dotnet test .\LabAuthServer.slnx -c Release --no-build --nologo
```

## Current result

The current security-remediation validation completed with **184 passed, 0 failed, and 0 skipped** tests.

## Coverage areas

The tests cover configuration fail-closed behavior, LDAPS/UPN validation, LDAP filter escaping, group-to-role mapping, JWT claims and signing, public-key-only JWT validation, certificate validity/key-size/key-usage/duplicate-selection boundaries, issuer/audience/lifetime/algorithm/key-ID validation, 401/403 authorization behavior, login rate limiting, correlation IDs, safe `ProblemDetails`, encrypted SQL test configuration, SQL audit persistence, sensitive-data filtering, and concurrency boundaries.

## What the tests do not prove

Automated tests do not replace authorized environment validation. They do not prove that a target AD account can authenticate, that a target certificate private key is accessible to the runtime identity, that a DPAPI secret can be decrypted on a target server, or that an IIS deployment is currently running. Tests also do not establish an approved audit retention or purge process.

Tests use placeholders and isolated seams where possible. Do not add real passwords, tokens, private keys, or DPAPI contents to test fixtures.
