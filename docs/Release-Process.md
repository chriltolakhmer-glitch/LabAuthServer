# Release process

## Source version and build verification

Use `vMAJOR.MINOR.PATCH` tags. Record the exact source commit, SDK, dependency restore, Release build, test results and compatible Auth/API/Web versions. Create the release tag only at that tested commit; verify `git rev-parse <TAG>^{commit}` matches the recorded source SHA. Published tags and packages are immutable; corrections receive a new version.

`.github/workflows/build.yml` runs on pushes and pull requests targeting `main`, with read-only repository permissions on Windows. It checks out source, installs .NET SDK 10.0.400, restores, builds Release and runs the tests described below. Local verification may use a compatible .NET 10 SDK; Auth's `global.json` permits latest patch in its feature band. CI is validation only: it does not create tags, publish releases or deploy. A successful local run is not evidence that the hosted workflow ran.

## Test scope and external dependencies

The existing CI pipeline is retained under `build.yml` (renamed from `ci.yml`, avoiding duplicate runs). It runs deterministic unit and HTTP integration tests with `-m:1 --filter "Category!=SqlInfrastructure&Category!=LdapAcceptance"`. Serialization preserves the documented TEST-ISOLATION-1 workaround without weakening or retrying tests.

CI then provisions disposable hosted LocalDB using the existing SQL scripts and runs both `Category=SqlInfrastructure` tests. Missing tools, failed provisioning, failed tests or skipped/missing SQL results fail CI; this gate is not optional. Local source validation in this task does not provision SQL. The existing LocalDB-only trust setting is test-specific, not a production TLS recommendation.

The single real `Category=LdapAcceptance` case is excluded with an explicit workflow notice: hosted CI has no approved AD/LDAPS target or protected credentials. Follow [Testing](Testing.md) for authorized SQL and LDAP acceptance enablement. Ordinary tests must not access operational infrastructure.

Excluded/unsupported acceptance must be recorded as NOT RUN, never PASS. Provision explicitly authorized isolated targets before running infrastructure acceptance. Do not inject production credentials into pull-request workflows or bypass TLS/target guards. Required acceptance remains a release requirement even when hosted build validation passes.

## API and Postman acceptance

Every new, modified or removed API endpoint requires the corresponding Postman collection update before completion. Include method/path, headers, authentication, credential-free request examples and response/status assertions; retire obsolete requests and verify the removal contract. Web client expectations must follow consumed API contract changes.

Before release, run the relevant collections against the exact candidate artifact in an approved environment. Verify success, authentication failures, validation/errors and allowed/denied behavior separately for Reader, Operator and Administrator. Create only run-owned fixtures and verify cleanup. Record environment, artifact identity, executed role, assertion counts and cleanup outcome. Missing credentials or skipped cases remain NOT RUN. Keep secrets, tokens, cookies and raw sensitive run exports outside Git. Do not infer full role coverage from a single successful run.

## Artifact verification and deployment

Publish the tested source once to an external staging directory. Verify the package contains the intended binaries/static assets and no local runtime settings, private keys, credentials or test output. Record its SHA256 and verify the same checksum and contents at deployment; do not rebuild between acceptance and deployment. Keep production configuration external and protected. Database/schema compatibility is API-owned; Auth audit storage is separate and Web has no direct database dependency.

Follow the existing deployment runbook for the application. Confirm HTTPS, external configuration, runtime identity access and version compatibility. Keep source CI separate from IIS/deployment operations. A release needs both artifact verification and environment-specific smoke/acceptance evidence.

## Rollback requirement

Before deployment, retain the previous immutable package and checksum, compatible external configuration, and a documented rollback procedure. Identify the rollback trigger and verify the recovery path is available. Account for schema compatibility and required backups; application rollback does not automatically reverse database migrations. Web recycle/rollback loses in-memory sessions and requires login again. Verify health and the critical authenticated journey after rollback, and record whether rollback was exercised or only prepared.

For Auth, [Release Governance](Release-Governance.md) and its six-check solo-developer checklist remain authoritative. Postman acceptance supplies relevant test/smoke evidence; rollback readiness follows existing deployment procedures. This document adds no approval committee or publication authorization.
