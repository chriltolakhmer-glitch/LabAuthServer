# Phase 4.11 — GitHub CI Integration

Status: PLANNING ONLY. [Phase 4 README](Phase-4-README.md) | Previous: [4.10](Phase-4.10-Testing-Strategy.md).

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

- 4.10 test strategy defined.
- Existing workflow `.github/workflows/ci.yml` reviewed (TO BE CONFIRMED DURING IMPLEMENTATION).

## Inputs

- Current CI steps: restore, Release build, tests.
- Current baseline: 733 tests, 733 passed, 0 failed, 0 skipped, 0 warnings, 0 errors.

## Design decisions

| ID | Decision | Status | Reason |
| --- | --- | --- | --- |
| D4.11-1 | CI never receives the production vendor private signing key | FINAL (constraint) | Key custody |
| D4.11-2 | Licensing tests generate an ephemeral signing key per run | PROPOSED | No stored secret, no rotation burden |
| D4.11-3 | CI runs restore, Release build, all existing tests and licensing tests | FINAL (constraint) | No reduction in coverage |
| D4.11-4 | The licensing work must not reduce the 733-test baseline | FINAL (constraint) | Regression gate |
| D4.11-5 | No new CI secret is introduced unless explicitly approved and justified | PROPOSED | Minimizes exposure surface |

DECISION REQUIRED: Which test-key strategy CI uses. Options: (a) ephemeral key generated during the test run, (b) a committed test-only key pair clearly labelled, (c) a CI secret holding a test key, (d) a test certificate in a secret store. Recommendation: (a). It requires no secret, leaves no long-lived key material and cannot be accidentally reused in production.

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

## Files likely to change

- `.github/workflows/ci.yml` — only in the implementation phase that explicitly authorizes CI changes.
- TO BE CONFIRMED DURING IMPLEMENTATION: any test project reference needed so the new tests run in the existing job.

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