# Phase 5.6 — Governance Control Implementation

> IMPLEMENTATION IN PROGRESS — NOT COMPLETE

Status: PHASE 5.6 — IMPLEMENTATION IN PROGRESS. This record documents the Phase 5.6 implementation pass for the owner-approved Phase 5.5 governance controls. The repository-side immutable GitHub Actions SHA pinning is committed, pushed, and hosted-CI validated at commit `966fe71733e6bb8bbdce5c41267da88c1b4571ae` (run #10 / `34675530766`, conclusion `SUCCESS`); the Phase 5.6 documentation commit is hosted-CI validated at run #11 / `34676441908`, conclusion `SUCCESS`. The Phase 5.5 prerequisite gate was verified `SUCCESS` (run #9 / `34674729292`, commit `8d0c0e9557d876b4219ecfebbb3e0d886cb11df9`). For D2, the externally supplied prerequisite verification for workflow `LabAuthServer CI`, run #22 / `34689144991`, head `50723d2e6ae3e2153dc4d914cbc697db416df859`, status `COMPLETED`, conclusion `SUCCESS`, is accepted. For D3, the externally supplied prerequisite verification for workflow `LabAuthServer CI`, run #24 / `34691894674`, head `63564b1c4a0457c4c85d739bffa8344409d30e95`, status `COMPLETED`, conclusion `SUCCESS`, is accepted. A classic GitHub branch-protection rule for `main` was configured by the owner, but GitHub reports that it is not enforced for this private personal-account repository; no ruleset was created, no fork setting was changed, and no collaborator access was changed. D3 SSH signing is configured repository-locally and the new governance commit is signed. The repository remains private and no public workaround is authorized; see section 15.

## 1. Purpose

Phase 5.5 approved 12 repository, security, and contribution governance decisions. Phase 5.6 implements only the controls that are technically safe, platform-supported, and free of unresolved values.

Phase 5.5 owner approval recorded policy direction, not implemented control. This phase implements what can be implemented without inventing identities, contacts, or assignments, and records every control that remains blocked.

## 2. Scope

This phase covers:

- verification of the Phase 5.5 hosted CI gate
- inspection of current GitHub governance state
- implementation of the owner-approved GitHub Actions immutable SHA pinning decision
- classification of every approved control as implemented or blocked
- recording of remote GitHub settings changed, blocked, or not attempted
- rollback considerations for the repository-side change

## 3. Non-Goals

This phase does not include:

- changing GitHub repository settings
- enabling branch protection
- creating repository rulesets
- disabling repository forking
- changing collaborator or administrator access
- changing repository-level or workflow-level GitHub Actions permissions
- changing repository visibility
- creating tags or GitHub Releases
- generating GPG, SSH, or other commit-signing keys
- changing Git `user.name` or `user.email` outside the separately approved D2 repository-local identity implementation
- reassigning D6/D7 named governance roles beyond the current policy records
- rewriting Git history
- deployment

## 4. Verified Baseline

Verified locally before implementation:

- Repository root: `C:\Apps\LabAuthServer\Source\LabAuthServer`
- Branch: `main`
- `HEAD`: `8d0c0e9557d876b4219ecfebbb3e0d886cb11df9`
- `origin/main` synchronized: `0` ahead, `0` behind
- Working tree clean before this pass
- `git diff --check`: clean
- Local tags: none
- Commit identity on all commits: `unknown <Administrator@LAB.LOCAL>`
- Latest commit signature status: `N` (unsigned)

Owner-verified GitHub state carried forward from Phase 5.5:

- Repository visibility: `PRIVATE`
- Default branch: `main`
- `main` branch protection: classic rule configured by owner; GitHub reports `NOT ENFORCED`
- Required status checks on `main`: not enforced
- Repository forking: `ALLOWED`
- Rulesets: `UNAVAILABLE UNDER CURRENT PRIVATE-REPOSITORY PLAN`
- GitHub Releases: `NONE`

## 5. Phase 5.5 CI Gate

Target evidence:

| Item | Value |
| --- | --- |
| Commit | `8d0c0e9557d876b4219ecfebbb3e0d886cb11df9` |
| Hosted run | Run #9 |
| Run ID | `34674729292` |
| Required conclusion | `SUCCESS` |
| Verified conclusion | `SUCCESS` |

Owner-verified evidence for the prerequisite gate:

- Status: `COMPLETED`
- Conclusion: `SUCCESS`

The prerequisite CI gate is now satisfied on the CI-evidence side.

The D1 setting change was attempted through authenticated GitHub administration and rejected by GitHub with HTTP 422 because private-repository forking changes are restricted to org-owned private repositories. The failed request changed no setting; authentication was available and is not the blocker.

No tooling was installed, no token was requested, exposed, printed, or stored, and CI was not bypassed.

## 6. Control Implementation Results

| # | Approved Control | Classification |
| --- | --- | --- |
| 1 | Pull request required for normal `main` changes | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 2 | Successful LabAuthServer CI required before `main` update | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 3 | Force pushes prohibited on `main` | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 4 | Strongest supported private-repository protection mechanism | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 5 | Disable private forking | `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL` |
| 6 | Stable vendor-controlled commit identity | `OWNER APPROVED — IMPLEMENTED` |
| 7 | Mandatory SSH commit signing for future official history | `OWNER APPROVED — IMPLEMENTED` |
| 8 | Immutable GitHub Actions SHA pinning | `COMPLETE — IMPLEMENTED AND HOSTED-CI VALIDATED` |
| 9 | Security contact policy and value | `OWNER APPROVED — IMPLEMENTED` |
| 10 | Commercial contact policy and value | `OWNER APPROVED — IMPLEMENTED` |
| 11 | Repository administration policy and named assignments | `OWNER APPROVED — IMPLEMENTED` |
| 12 | Security Response Owner policy and named identity | `OWNER APPROVED — IMPLEMENTED` |

The owner configured a classic branch-protection rule for `main`. GitHub reports that the rule is **NOT ENFORCED** because enforcement for this private repository requires moving it to a GitHub Team or Enterprise organization account. Therefore controls 1–4 are classified `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION`; they must not be described as implemented, complete, closed, or satisfied. The owner confirmed the Phase 5.5 forking direction: private forking should be disabled unless a specific controlled vendor workflow later requires it. An authenticated GitHub administrative attempt to set `allow_forking = false` was rejected with HTTP 422: `Allow forks setting can only be changed on org-owned private repositories`. D1 is therefore `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL`; the observed setting remains `ALLOWED`, authentication is not the blocker, and retrying the same operation under the current ownership model will not resolve it. The repository remains private and no public-visibility workaround or ownership transfer is authorized.

## 7. Repository-Side Change — Immutable Actions SHA Pinning

### Approved control

Phase 5.5 decision 8: migrate third-party GitHub Actions references toward immutable commit SHA pinning after controlled review and validation.

### Previous state

`.github/workflows/ci.yml` referenced all external Actions by moving major-version tags:

```
actions/checkout@v4
actions/setup-dotnet@v4
actions/cache@v4
```

A moving major tag can be repointed upstream, so the exact executed code is not fixed by the workflow file alone.

### Target state

Each external Action reference is pinned to the full 40-character commit SHA that the `v4` tag currently resolves to, with a human-readable `# v4` comment retained.

### Resolution and verification method

Each SHA was resolved from official upstream GitHub metadata, then cross-verified by reading the commit from the same repository:

| Action | Official repository | Resolved `v4` commit SHA | Verification |
| --- | --- | --- | --- |
| `actions/checkout` | `actions/checkout` | `11d5960a326750d5838078e36cf38b85af677262` | commit read back from same repository; tag ref `refs/tags/v4`; `object.type = commit` |
| `actions/setup-dotnet` | `actions/setup-dotnet` | `67a3573c9a986a3f9c594539f4ab511d57bb3ce9` | commit read back from same repository; tag ref `refs/tags/v4`; `object.type = commit` |
| `actions/cache` | `actions/cache` | `0057852bfaa89a56745cba8c7296529d2fc39830` | commit read back from same repository; tag ref `refs/tags/v4`; `object.type = commit` |

No SHA was invented, estimated, or copied from a third-party mirror. Each resolved tag ref reported `object.type = commit`, so no annotated-tag dereference step was required.

### Resulting workflow content

- `actions/checkout@11d5960a326750d5838078e36cf38b85af677262 # v4`
- `actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4`
- `actions/cache@0057852bfaa89a56745cba8c7296529d2fc39830 # v4`

### Behavior-preservation review

- Pinning targets the exact commit that `@v4` resolves to today, so current workflow behavior is unchanged.
- The pin freezes the Action version until deliberately reviewed and updated.
- Workflow `permissions` remain `contents: read`.
- No secret was added, and no production licensing private key is present.
- Restore, build, disposable LocalDB provisioning, and test commands are unchanged.
- No step was added, removed, reordered, or renamed.
- Only the three Action references changed.

### Validation performed

- `git diff --check`: clean
- Workflow inspected after edit; diff limited to the three Action reference lines

### Hosted execution validation

The pinned Action references were validated by hosted workflow execution:

| Item | Value |
| --- | --- |
| Commit | `966fe71733e6bb8bbdce5c41267da88c1b4571ae` |
| Hosted run | Run #10 |
| Run ID | `34675530766` |
| Conclusion | `SUCCESS` |

Result: `IMPLEMENTED AND HOSTED-CI VALIDATED`.

Note on validation scope: a SHA-pin change is a hosted-runner-only reference change. A local `dotnet restore` / `build` / `test` does not execute or resolve the pinned Action references, so a local build or test run would not validate this change. Hosted CI run #10 resolved and executed the pinned references successfully, which is the definitive validation for this control.

## 8. Remote GitHub Changes

Settings actually changed remotely: **A classic `main` branch-protection rule was configured by the owner.**

Settings attempted but blocked: **D1 private-forking disablement** — the authenticated API request was rejected by GitHub with HTTP 422 because `allow_forking` can only be changed on org-owned private repositories.

Remote control classification:

| Control | Current State | Classification |
| --- | --- | --- |
| `main` branch protection | Classic rule exists; GitHub reports `NOT ENFORCED` | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Pull-request enforcement | Not enforced | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Required CI enforcement | Not enforced | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Force-push restriction | Not enforced | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Branch deletion restriction | Not enforced | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Fork disablement | API request rejected; `allow_forking = true` | `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL` |
| Rulesets | Unavailable | `BLOCKED — PLATFORM / PLAN LIMITATION` (verified) |

Settings not changed and still at their verified prior state:

- repository visibility: `PRIVATE`
- classic `main` branch-protection rule: configured by owner; GitHub reports `NOT ENFORCED`
- required status checks on `main`: not enforced
- repository forking: `ALLOWED`
- rulesets: `UNAVAILABLE UNDER CURRENT PRIVATE-REPOSITORY PLAN`
- GitHub Releases: `NONE`

No affected remote control is reported as enforced. The D1 post-attempt state was independently read back as `private = true` and `allow_forking = true`.

### Remote implementation matrix

| Control | Current | Target | Capability | Implementation State |
| --- | --- | --- | --- | --- |
| Main protection | Classic rule exists; GitHub reports `NOT ENFORCED` | Protect `main` | Owner-verified rule; exact saved options not independently recorded | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| PR requirement | Not enforced | PR required for normal `main` changes | Covered by owner-configured rule; exact saved option not independently recorded | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Approving review | Not independently recorded | Review where supported | Exact saved option not independently recorded | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Required CI check | Not enforced | `Build and Test (Release)` required before normal `main` update | Covered by owner-configured rule; exact saved option not independently recorded | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Force push | Not enforced | Prohibited on `main` where supported | Covered by owner-configured rule; exact saved option not independently recorded | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Branch deletion | Not enforced | Prohibited on `main` where supported | Covered by owner-configured rule; exact saved option not independently recorded | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Forking | `ALLOWED` | Disable unless a specific controlled vendor workflow requires it | GitHub rejects the setting change for personally owned private repositories | `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL` |
| Rulesets | Unavailable | Not the selected mechanism | `BLOCKED — PLAN LIMITATION` (verified) | `BLOCKED — PLAN LIMITATION` |
| Immutable Actions SHA pinning | Pinned `v4` SHAs | Immutable SHA pins | Repository-side, supported | `IMPLEMENTED AND HOSTED-CI VALIDATED` |

No row is marked implemented without re-read evidence. Only the repository-side SHA pinning row has such evidence.

### Branch-protection capability

Classification: `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION`.

- GitHub documentation states that protected branches for private repositories require GitHub Pro, GitHub Team, GitHub Enterprise Cloud, or GitHub Enterprise Server.
- The owner verified that a classic `main` branch-protection rule exists and that GitHub reports it as `NOT ENFORCED`.
- GitHub states that enforcement for this private repository requires moving it to a GitHub Team or Enterprise organization account.
- The repository remains private. Making it public or transferring it to an organization is not authorized at this time.

### Required CI check context

Required CI check context: `Build and Test (Release)` — VERIFIED.

Evidence:

| Item | Value |
| --- | --- |
| Workflow | `LabAuthServer CI` |
| Run | #10 / `34675530766` |
| Job / check | `Build and Test (Release)` |
| Job ID | `103504610220` |
| Conclusion | `SUCCESS` |

Clarification: the workflow name and the required check/job context are not the same field.

- Workflow name: `LabAuthServer CI`
- Job/check context: `Build and Test (Release)`

The owner-verified GitHub Actions jobs API identifies the successful job as `Build and Test (Release)`, which is the check context associated with the configured rule. GitHub reports enforcement as **NOT ENFORCED** for this private repository. The verified context itself is unchanged and remains `Build and Test (Release)`.

### Forking control

- Current: `ALLOWED`.
- Approved target: disable unnecessary private forking unless a specific controlled vendor workflow requires it.
- This rule does not independently establish or change the repository forking setting.
- Classification: `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL`.
- Repository visibility was not changed. Authentication is available; GitHub does not permit this setting change for the current personally owned private-repository model.

## 9. Rollback Considerations

The implemented repository-side controls are immutable Action SHA pinning, the owner-approved D2 repository-local Git identity, and D3 SSH commit signing. D3 uses an Ed25519 signing key owned and held by `ALOT`, with fingerprint `SHA256:GwFTsR04jnXRbLiRnaEhjiGEgnx36rtuWmxFubZHXyo`; the private key remains outside the repository. Git signing is configured repository-locally with `gpg.format=ssh`, the dedicated signing-key path, and `commit.gpgSign=true`. Previous history was not rewritten.

- No remote rollback is required, because no remote setting was changed.
- Rollback for the pins is a normal forward commit that restores the previous major-version references (`@v4`) for the affected Action only, then re-verifies before re-pinning. No force push, reset, rebase, or history rewrite is involved or authorized.
- The pins are additive and reversible; hosted CI run #10 confirms they resolve correctly.
- No key, secret, tag, release, or deployment is involved, so no credential rotation or release retraction is required.

## 10. Remaining Governance Work

Owner-approved and not yet executed:

- configure `main` branch protection using the strongest supported private-repository mechanism
- require the LabAuthServer CI check before normal `main` updates
- enforce the pull-request workflow for normal `main` changes
- prohibit force pushes to `main`
- resolve the owner-approved private-forking policy through a separately authorized ownership-model decision; no retry is actionable under the current model
- maintain the owner-approved SSH signing-key rotation and revocation policy
- obtain the exact `<SECURITY_CONTACT>` value
- obtain the exact `<COMMERCIAL_CONTACT>` value
- record exact named repository administrator assignments where required
- record the exact named identity if repository convention requires one for the initial Security Response Owner policy

Blocking prerequisites:

- an organization-owned private-repository model is required before `allow_forking` can be changed; authentication is available and is not the D1 blocker
- the GitHub plan must be upgraded, or the required private-repository administration features otherwise obtained, before the deferred remote controls can be attempted
- contact and named-assignment values must be supplied by the owner; they must not be invented

The Phase 5.5 hosted CI gate is now verified `SUCCESS` and is no longer a blocking prerequisite.

Current decision-record prerequisite: the externally supplied hosted result for workflow `LabAuthServer CI`, run `#21` / `34687199205`, reports `COMPLETED` / `SUCCESS` for head `44b75036f68a767a92d78e608732c21b633c9b83`. The authenticated `gh` session is available; the documented D1 API attempt was rejected by the repository ownership model, not by authentication.

The configured remote governance controls are `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION`. They are not enforced and must be revisited if the repository ownership model changes.

## 11. Platform and Tooling Limitations

Two distinct limitations apply, and they must not be conflated.

### Tooling and platform distinction

- GitHub CLI (`gh`) is installed and available in the implementation environment.
- The CLI is authenticated as the repository owner account, and authenticated GitHub administration is available.
- No `GH_TOKEN` / `GITHUB_TOKEN` credential is present; none was requested, created, or stored.
- The D1 repository-setting request was rejected by GitHub with HTTP 422 because `allow_forking` can only be changed on org-owned private repositories.
- Effect: D1 is blocked by the current private personal-account ownership model, not by CLI availability or authentication. Retrying the same request under the current ownership model is not actionable.

### Verified plan limitation — rulesets

- Repository rulesets returned `UNAVAILABLE UNDER CURRENT PRIVATE-REPOSITORY PLAN`, with the GitHub API response: "Upgrade to GitHub Pro or make this repository public to enable this feature."
- This is specific to the current private repository and account/plan; it is not a universal statement about rulesets.
- Traditional branch protection is configured by the owner, but GitHub reports it as `NOT ENFORCED` under the current private personal-account ownership model.
- Making the repository public to obtain a governance feature is not approved and was not done. Repository privacy remains the approved business decision.

## 12. Security Check

No private key, signing key, GPG key, SSH key, password, token, credential, customer license, customer credential, or production secret was generated, added, printed, or stored. No authentication token was displayed. The only repository-side change is immutable SHA pinning in the CI workflow, which adds no secret and does not change the declared `contents: read` permission boundary.

## 13. Phase 5.6 Completion Checklist

- [x] Repository safety baseline verified.
- [x] Phase 5.5 CI gate verified `SUCCESS` and recorded.
- [x] Current GitHub state preserved from owner-verified Phase 5.5 evidence.
- [x] Immutable Actions SHA pinning implemented with upstream-verified SHAs.
- [x] Immutable Actions SHA pinning committed, pushed, and hosted-CI validated (run #10 / `34675530766`).
- [x] Action pinning security review performed.
- [x] Behavior-preservation review performed.
- [x] Remote GitHub settings change correctly withheld.
- [x] Remote implementation matrix recorded.
- [x] Classic `main` branch-protection rule recorded as configured by the owner and `NOT ENFORCED` by GitHub.
- [x] Required CI check context verified and recorded as `Build and Test (Release)` (run #10 / `34675530766`, job ID `103504610220`, conclusion `SUCCESS`).
- [ ] `main` branch protection enforced. (`BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION`; currently `CONFIGURED BUT NOT ENFORCED`)
- [ ] Required CI status check enforced. (`BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION`; currently `CONFIGURED BUT NOT ENFORCED`)
- [ ] Pull-request requirement enforced. (`BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION`; currently `CONFIGURED BUT NOT ENFORCED`)
- [ ] Approving-review requirement enforced. (`BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION`; currently `CONFIGURED BUT NOT ENFORCED`)
- [ ] Force-push prohibition enforced. (`BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION`; currently `CONFIGURED BUT NOT ENFORCED`)
- [ ] Branch-deletion restriction enforced. (`BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION`; currently `CONFIGURED BUT NOT ENFORCED`)
- [ ] Repository forking disabled. (`OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL`; current state `ALLOWED`)
- [x] Exact Git identity values supplied and applied: `ALOT <chriltola.khmer@gmail.com>`, repository-local, same identity for governance/release commits. (`OWNER APPROVED — IMPLEMENTED`; previous history was not rewritten)
- [x] SSH commit signing implemented: Ed25519, owner/custody/recovery/rotation owner `ALOT`, fingerprint `SHA256:GwFTsR04jnXRbLiRnaEhjiGEgnx36rtuWmxFubZHXyo`, GitHub signing-key registration verified, repository-local configuration verified, and signed commit created. (`OWNER APPROVED — IMPLEMENTED`)
- [x] Security contact value supplied: `chriltola.khmer@gmail.com` with owner-approved shared-mailbox exception. (`OWNER APPROVED — IMPLEMENTED`)
- [x] Commercial contact value supplied: `chriltola.khmer@gmail.com` with owner-approved shared-mailbox exception. (`OWNER APPROVED — IMPLEMENTED`)
- [x] Exact repository administrator assignments recorded. (`OWNER APPROVED — IMPLEMENTED`)
- [x] Exact Security Response Owner identity recorded where required. (`OWNER APPROVED — IMPLEMENTED`)
- [x] Rollback considerations recorded.
- [x] Remaining governance work recorded.
- [x] No secret, key, or credential introduced.
- [x] No remote governance setting change performed; D1 API request was rejected without changing state.

Phase 5.6 is **not complete**. Critical governance controls remain unimplemented.

## 14. Current Status

Phase 5.6 — IMPLEMENTATION IN PROGRESS. The repository-side immutable Actions SHA pinning control is committed, pushed, and hosted-CI validated at commit `966fe71733e6bb8bbdce5c41267da88c1b4571ae` (run #10 / `34675530766`, conclusion `SUCCESS`); the Phase 5.6 documentation commit is hosted-CI validated at run #11 / `34676441908`, conclusion `SUCCESS`. The Phase 5.5 prerequisite gate was verified `SUCCESS` (run #9 / `34674729292`). The required CI check context is verified as `Build and Test (Release)` (job ID `103504610220`); GitHub reports the configured classic rule as `NOT ENFORCED`. Remote GitHub governance controls remain `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION`. D2 is `OWNER APPROVED — IMPLEMENTED` with repository-local identity `ALOT <chriltola.khmer@gmail.com>` for future governance and release commits; previous history was not rewritten. D3 is `OWNER APPROVED — IMPLEMENTED` with repository-local SSH signing, GitHub registration of the Ed25519 public key, fingerprint `SHA256:GwFTsR04jnXRbLiRnaEhjiGEgnx36rtuWmxFubZHXyo`, and a successfully signed governance commit. D4 and D5 are `OWNER APPROVED — IMPLEMENTED` with the approved shared-mailbox exception: `chriltola.khmer@gmail.com` for security and commercial/evaluation intake. D6 and D7 are `OWNER APPROVED — IMPLEMENTED` with the approved named assignments for `ALOT` as repository administrator and Security Response Owner. The repository remains private, no public-visibility workaround was used, and no transfer is authorized at this time. Phase 5.6 remains in implementation progress. Phase 5.7 is `OWNER APPROVED — 2026-09-12` and remains documentation-only.

## 15. CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION

Owner-verified state recorded on 2026-09-12:

> A classic branch-protection rule for `main` is configured by the owner, but GitHub reports that it is not enforced because this private repository must be moved to a GitHub Team or Enterprise organization account for enforcement. The repository must remain private. No public-visibility workaround or transfer is authorized at this time.

Affected controls — configured but not enforced:

| # | Control | State |
| --- | --- | --- |
| 1 | Private-repository branch protection | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 2 | Pull-request requirement | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 3 | Approving-review requirement | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 4 | Required CI enforcement | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 5 | Force-push restriction | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 6 | Branch-deletion restriction | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| 7 | Forking governance | `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL` |

Constraints and preserved facts:

- The repository must remain `PRIVATE`. Making it public is not an authorized workaround for any deferred control.
- Verified required CI context remains `Build and Test (Release)` (workflow `LabAuthServer CI`, job ID `103504610220`).
- Current factual state: classic `main` rule exists and is `NOT ENFORCED`; forking remains `ALLOWED` because GitHub rejects the change under the current personally owned private-repository model; repository remains `PRIVATE`.
- Rulesets remain `BLOCKED — VERIFIED PLAN LIMITATION`, separate from this owner decision.
- The affected controls are **not** enforced, complete, closed, or satisfied.
- These controls must be revisited if the repository ownership model changes.
- No public-visibility workaround or ownership transfer was authorized or used.

## 16. Current Status (Deferral Note)

Phase 5.6 is **not fully complete** while the controls in section 15 remain not enforced. Phase 5.7 is `OWNER APPROVED — 2026-09-12`; that approval did not resolve or override the Phase 5.6 platform/ownership-model blockers.

## 17. Remaining Blocker Matrix

Every incomplete Phase 5.6 item has a precise classification below. No row represents an implemented or enforced control unless explicitly marked `COMPLETE`.

| Unresolved item | Classification | What is needed to unblock it |
| --- | --- | --- |
| Enforced private-repository branch protection | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` | A supported private-repository ownership/plan model that enforces the configured `main` protection rule; the repository must remain private. |
| Required CI enforcement | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` | The same enforced private-repository protection capability, with `LabAuthServer CI` / `Build and Test (Release)` required. |
| Pull-request enforcement | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` | The same enforced private-repository protection capability for normal `main` changes. |
| Approving-review requirement | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` | The same enforced private-repository protection capability with the approved review requirement enabled. |
| Force-push prohibition | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` | The same enforced private-repository protection capability that restricts force pushes. |
| Branch-deletion restriction | `BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION` | The same enforced private-repository protection capability that protects `main` from deletion. |
| Repository forking policy | `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL` | The desired policy is approved, but GitHub requires an org-owned private repository before `allow_forking` can be changed. |
| Immutable GitHub Actions SHA pinning | `COMPLETE` | No further action for this reconciliation; preserve the validated immutable pins. |
