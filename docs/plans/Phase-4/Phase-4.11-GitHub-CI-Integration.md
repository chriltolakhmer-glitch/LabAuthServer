# Phase 4.11 — GitHub CI Integration

Status: COMPLETE — CI VERIFIED, NO WORKFLOW CHANGE REQUIRED. [Phase 4 README](Phase-4-README.md) | Previous: [4.10](Phase-4.10-Testing-Strategy.md).

## Objective

Define how CI builds and tests licensing without receiving the production vendor private signing key.

## Scope

In scope:

- Options for test signing material.
- Required CI steps.
- Secret-handling constraints.
- Baseline preservation.

Out of scope:

- Changing CI as part of this documentation task.
- Release signing or artifact publication.
- Production key management (4.12).

## Why it exists

A CI pipeline that holds the vendor signing key converts every workflow run, fork and log into a key-exposure surface. CI must prove licensing works without that key.

## Prerequisites

- 4.10 test strategy defined (COMPLETE).
- Existing workflow `.github/workflows/ci.yml` reviewed (COMPLETE — see Implementation record).

## Inputs

- Current CI steps: restore, Release build, tests (confirmed in `.github/workflows/ci.yml`).
- Pre-Phase-4 baseline: 733 tests, 733 passed, 0 failed, 0 skipped, 0 warnings, 0 errors.
- Phase 4.10 baseline: 991 tests, 991 passed, 0 failed, 0 skipped, 0 warnings, 0 errors.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.11-1 | CI never receives the production vendor private signing key | FINAL (constraint) | Key custody |
| D4.11-2 | Licensing tests generate an ephemeral signing key per run | APPROVED (O-16 resolved, D-19) | No stored secret, no rotation burden |
| D4.11-3 | CI runs restore, Release build, all existing tests and licensing tests | FINAL (constraint) | No reduction in coverage |
| D4.11-4 | The licensing work must not reduce the 733-test baseline | FINAL (constraint) | Regression gate |
| D4.11-5 | No new CI secret is introduced unless explicitly approved and justified | FINAL (constraint) | Minimizes exposure surface |

DECISION RESOLVED (O-16): CI uses option (a), an ephemeral signing key generated during the test run. No committed test key pair, no CI secret and no test certificate in a secret store are used. Rationale: no stored secret, no rotation burden, and no key material that can be reused in production.

## Proposed architecture

Evaluated options:

| Option | Secret exposure | Reproducibility | Misuse risk | Recommendation |
| --- | --- | --- | --- | --- |
| Ephemeral key per run | None | High (deterministic tests, non-deterministic key) | Very low | Preferred |
| Committed test-only key pair | None in CI, but key exists in the repository forever | High | A reviewer may mistake it for usable | Acceptable with explicit labelling |
| CI secret holding a test key | Secret in CI | High | Secret management burden, rotation | Not preferred |
| Test certificate in a secret store | Secret in CI | High | Same as above plus certificate lifecycle | Not preferred |

CI pipeline shape (conceptual, not a workflow edit):

1. Checkout.
2. Restore dependencies.
3. Build in Release.
4. Run the existing test suite unchanged.
5. Run the licensing test suite with an ephemeral test key.
6. Report total, passed, failed and skipped counts.
7. Fail the job if the baseline is not preserved or if any test fails.

Prohibitions:

- Do not add a workflow step that writes key material to disk beyond the test's temporary scope.
- Do not echo license bodies or key material into logs.
- Do not upload licenses as build artifacts.
- Do not add a repository secret containing a production signing key.

## Files changed

None. The existing `.github/workflows/ci.yml` already satisfies Phase 4.11, so no workflow or project file was modified. The licensing tests already live in `tests/LabAuthServer.UnitTests/Licensing/`, which is part of `LabAuthServer.slnx`, so the existing `dotnet test LabAuthServer.slnx` step runs them without any project reference change.

## Files that must NOT change

- Production configuration files.
- Deployment workflows.
- Any secret or environment definition holding production credentials.

## Implementation steps

1. Resolve the test-key strategy.
2. Confirm the existing workflow's build and test steps.
3. Add the licensing test execution only if it is not already covered by the existing test command.
4. Verify the reported test counts against the baseline.
5. Record the CI evidence.

## Security considerations

- Fork pull requests must not gain access to secrets; the ephemeral strategy satisfies this by requiring none.
- Logs must not contain license bodies, customer names or key material.
- Artifact upload must exclude any license or key file.

## Failure cases

- A production key added as a repository secret "for convenience".
- Licensing tests skipped in CI because they were placed in a separate project not referenced by the build.
- Baseline test count silently reduced.
- Test output printing a full license.

## Testing requirements

- CI job runs green with no licensing secret configured.
- Test count report shows baseline plus new tests.
- A deliberate failure injection confirms the job fails when a licensing test fails.
- Log inspection confirms no key or license content is printed.

## Acceptance criteria

- CI documented as running restore, Release build, existing tests and licensing tests.
- No production private key present anywhere in CI.
- Baseline preserved or increased.
- Test-key strategy recorded with rationale.

## Rollback considerations

Reverting the CI change restores the previous workflow. Because licensing tests are additive, their removal does not affect the existing suite.

## Evidence to record

- Workflow diff (in the implementation phase).
- Test count report before and after.
- Confirmation that no new secret was added.

## Git/commit strategy

- Suggested message: `ci(licensing): add licensing tests without vendor private key`.
- No commit or push without explicit authorization.

## Dependencies on previous phases

- Requires 4.10.

## Risks

- Accidental introduction of a production secret into CI.
- Test project not wired into the build, giving false confidence.
- Job duration growth from added tests.

## Deferred items

- Separate licensing-only workflow.
- Code signing of release artifacts.
- Automated release gating on 4.15.

## Implementation record (2026-09-11)

Status: COMPLETE — CI CONFIGURATION VALIDATED LOCALLY. A GitHub-hosted run was not executed or verified.

### Existing CI baseline

`.github/workflows/ci.yml` ("LabAuthServer CI") is the only workflow in `.github/workflows/`.

| Property | Value |
| --- | --- |
| Triggers | `push` to `main`, `pull_request` targeting `main` |
| Permissions | `contents: read` (workflow level, least privilege) |
| Runner | `windows-latest` |
| .NET SDK | `10.0.400` via `actions/setup-dotnet@v4` — matches `global.json` (`10.0.400`, `rollForward: latestPatch`) |
| Cache | `actions/cache@v4` over `~/.nuget/packages`, keyed on `NuGet.Config`, `Directory.Build.props`, `global.json` |
| Restore | `dotnet restore LabAuthServer.slnx` |
| Build | `dotnet build LabAuthServer.slnx --configuration Release --no-restore` |
| Test | `dotnet test LabAuthServer.slnx --configuration Release --no-build --logger "console;verbosity=normal"` |
| Job | `build-and-test` / "Build and Test (Release)" |

### CI review conclusion

The existing workflow already satisfies every Phase 4.11 requirement. No change was made.

| Phase 4.11 requirement | Status | Evidence |
| --- | --- | --- |
| Restore | Covered | `dotnet restore LabAuthServer.slnx` |
| Release build | Covered | `dotnet build ... --configuration Release --no-restore` |
| Unit tests | Covered | `dotnet test` over the solution |
| Integration tests | Covered | Same test step; `LabAuthServer.IntegrationTests` is in `LabAuthServer.slnx` |
| Licensing tests | Covered | `tests/LabAuthServer.UnitTests/Licensing/` is in `LabAuthServer.UnitTests`, a solution project |
| Security-sensitive regression tests | Covered | Phases 4.3–4.10 licensing suites run in the same job |
| Test fixture behavior | Covered | `LicenseTestFixture` generates ephemeral keys per run; no shared state |
| No private-key requirement | Covered | No secret, no key file, no key material referenced by the workflow |
| No production certificate requirement | Covered | No certificate step or file reference |
| No production configuration requirement | Covered | Workflow references no `appsettings` file or environment secret |
| No external licensing service requirement | Covered | Licensing tests are offline; no network step beyond NuGet restore |
| No deployment | Covered | No deploy, publish or release step |
| No publishing of secrets | Covered | No secret is defined or echoed |
| Test failure behavior | Covered | `dotnet test` returns a non-zero exit code on failure; the job fails |

### Build and test evidence (local, Release)

- `dotnet build LabAuthServer.slnx -c Release --no-restore` → Build succeeded, 0 warnings, 0 errors.
- `dotnet test LabAuthServer.slnx -c Release --no-build` → Unit 745 passed, Integration 246 passed; total 991, 0 failed, 0 skipped.
- Baseline (733 tests, 733 passed) is preserved and exceeded; the +258 tests are the Phase 4.1–4.10 licensing suites.
- Known integration-test output: the LDAP deadline and cancellation tests emit `AuthenticationDeadlineExceeded` and `CallerCancelled` diagnostic log lines by design. These are expected diagnostics from passing tests, not failures. All 246 integration tests passed. The tests were not weakened and no retry was added.

### Licensing test matrix mapped into CI

All rows below are implemented in Phase 4.10 suites and run under the single CI test step; no test is duplicated in the workflow.

| Matrix area | Suite |
| --- | --- |
| Parser validation, duplicates, unknown properties, UTF-8/BOM, Base64, JSON depth, oversized input | `JsonLicenseDocumentParserTests`, `Phase45LicenseValidatorTests` |
| Signature/payload/keyId/algorithm tampering, unknown key, key rotation, RSA key-size boundaries | `RsaPssLicenseSignatureVerifierTests`, `Phase43CryptographicVerificationTests`, `Phase49TamperAndAbuseResistanceTests` |
| Product/edition validation, feature/limit validation | `Phase45LicenseValidatorTests`, `Phase46FeatureAndEditionEnforcementTests` |
| Expiration/perpetual, clock skew, restricted Community behavior, fail-closed | `Phase47ExpirationAndGracePeriodTests`, `Phase46FeatureAndEditionEnforcementTests` |
| Issuer/parser/verifier round trip, deterministic issuer serialization | `Phase44LicenseIssuerTests`, `Phase410TestingStrategyTests` |
| Private-key absence, network-dependency absence, regression guarantees | `Phase410TestingStrategyTests` |

### Ephemeral test-key strategy

O-16 is resolved. CI uses an ephemeral RSA signing key generated in-process per test run (`LicenseTestFixture`). No committed test key pair, no CI secret and no test certificate in a secret store is used. No production signing key exists in CI, and CI cannot issue a production license.

### .NET SDK / runtime assumption

The workflow pins .NET SDK `10.0.400`, which matches `global.json`. No SDK or runtime change was made for Phase 4.11. Target framework is `net10.0`.

### GitHub permission model

The workflow keeps the existing `permissions: contents: read`. No `contents: write`, `packages: write`, `pull-requests: write`, `id-token: write` or `deployments` permission is present or added. No release or deployment automation was added.

### Action / dependency review

| Action | Version | Purpose | Classification |
| --- | --- | --- | --- |
| `actions/checkout` | `v4` | Check out the repository | First-party (GitHub) |
| `actions/setup-dotnet` | `v4` | Install the pinned .NET SDK | First-party (GitHub) |
| `actions/cache` | `v4` | Cache NuGet packages | First-party (GitHub) |

No third-party or unpinned action was added. No new NuGet package was introduced.

### Secret / private-key review

- No secret is defined or referenced in the workflow.
- No `BEGIN PRIVATE KEY`, `BEGIN RSA PRIVATE KEY`, `ExportRSAPrivateKey`, `ExportPkcs8PrivateKey`, `.pfx`, `.p12` or certificate material is present in `.github/`.
- No password, token, credential or production license material is present in the workflow.
- `.gitignore` excludes `*.pfx`, `*.p12`, `*.key`, `secrets.json` and `appsettings.Local.json`.

### Issuer boundary

The issuer (`tools/LabAuthServer.LicenseIssuer/`) is outside `LabAuthServer.slnx` and is not referenced by any server project. `LabAuthServer.UnitTests` references it only so issuer tests can run; it is not part of the server runtime graph and cannot emit production licenses in CI.

### Security findings

| ID | Finding | Severity | Disposition |
| --- | --- | --- | --- |
| CI-1 | Existing workflow already meets Phase 4.11 with no change; no new exposure introduced | Informational | Accepted |
| CI-2 | Actions are pinned to major-version tags (`@v4`), not commit SHAs | Low | Recorded as a recommendation; the repository has no approved SHA-pinning policy, so no change was made |

### Deferred items (outside Phase 4.11 scope)

| Item | Reason |
| --- | --- |
| Separate licensing-only workflow | Existing single job already runs the licensing tests; a second workflow would duplicate restore/build |
| SHA-pinning of actions | No approved repository policy; recorded as CI-2 |
| Code signing of release artifacts | Outside CI validation scope |
| Release gating on Phase 4.15 | Phase 4.15 not started |
| Online activation, revocation, machine binding, HSM/vault, production key provisioning, deployment automation, release publishing, license management UI, runtime feature enforcement wiring, operator abuse logging, production signing-key generation | Out of Phase 4.11 scope |

### Final verification result

- Release build: succeeded, 0 warnings, 0 errors.
- Full test suite: 991 passed, 0 failed, 0 skipped (baseline 733 preserved).
- `git diff --check`: clean.
- No production private key, certificate, credential or production license material was introduced.
- No production signing capability and no deployment capability was introduced.
- CI configuration validated locally; GitHub-hosted run not executed/verified.