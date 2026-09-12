# Phase 5.6 - Owner Decision Packet

> DOCUMENTATION-ONLY DECISION PREPARATION — PHASE 5.6 REMAINS IMPLEMENTATION IN PROGRESS

## 1. Purpose

This packet records the remaining Phase 5.6 governance decisions for separate owner review and the owner-authorized D2 and D3 implementations. It records current evidence, recommendations, tradeoffs, and unresolved owner-input fields. It does not approve, implement, or configure any decision other than the separately authorized D2 repository-local identity and D3 SSH signing.

Phase 5 remains `PHASE 5 IN PROGRESS — PHASE 5.6 REMAINS OPEN`. Phase 5.6 remains `IMPLEMENTATION IN PROGRESS`. The repository remains `PRIVATE`. Remote governance controls remain `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION`.

## 2. Current Verified Baseline

| Item | Current value |
| --- | --- |
| Branch | `main` |
| HEAD before this update | `d79734349ecbf7f0569299a38181675f09387d43` |
| HEAD commit | `Prepare Phase 5.6 owner decision packet` |
| Hosted validation | `LabAuthServer CI`, run `#20`, run ID `34686970661`, head `d79734349ecbf7f0569299a38181675f09387d43`, `completed` / `success` |
| Repository visibility | `PRIVATE` |
| Phase 5 status | `PHASE 5 IN PROGRESS — PHASE 5.6 REMAINS OPEN` |
| Phase 5.6 status | `IMPLEMENTATION IN PROGRESS` |
| Main governance state | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Forking | `ALLOWED` (`allow_forking = true`); disablement is blocked by the repository ownership model |
| Git identity before D2 implementation | `unknown <Administrator@LAB.LOCAL>` |
| Latest commit signature | `N` (unsigned) |
| Security contact | `chriltola.khmer@gmail.com` |
| Commercial contact | `chriltola.khmer@gmail.com` |

The hosted validation result above is accepted as externally verified evidence. Local `gh` authentication is not required to re-prove it.

Completed Phase 5.6 control preserved by this packet: immutable GitHub Actions SHA pinning, already hosted-CI validated. The CI workflow remains unchanged.

## 3. Decisions Required

### D1 - Repository Forking

**Current status:** `OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL`.

**Current observed state:** Forking is `ALLOWED`.

**Existing policy evidence:**

- Phase 5.5 section 5 records forking as `ALLOWED` and recommends disabling it unless a specific controlled vendor workflow requires forks.
- Phase 5.5 section 13 states: `OWNER APPROVED - 2026-09-12. Forking should be disabled unless a specific controlled vendor workflow later requires it.` It also states that the current setting remains `ALLOWED` and disablement is a future operational action.
- The earlier Phase 5.6 record classified the item as `OWNER DECISION REQUIRED` and recorded that no fork setting was changed; the current owner-approved classification is now platform-blocked based on the verified API rejection.

The owner has resolved the Phase 5.5 / Phase 5.6 consistency issue by confirming that the Phase 5.5 direction is the operative policy. An authenticated GitHub administrative attempt was made, but GitHub rejected it because private-forking changes are restricted to organization-owned private repositories. The setting remains unchanged at `ALLOWED`; authentication is not the blocker, and retrying the same API operation under the current ownership model will not resolve it.

**Security benefit of disabling unnecessary private forking:** reduces additional private source copies, limits source-access cleanup obligations, and narrows proprietary-source exposure.

**Operational downside:** may block legitimate controlled vendor or organization workflows that depend on forks. Retaining forks is operationally flexible but expands the number of private copies and access-removal obligations.

**Platform / ownership consideration:** the current personally owned private-repository model does not permit changing `allow_forking`; GitHub returned HTTP 422: `Allow forks setting can only be changed on org-owned private repositories`. Moving the private repository to an appropriate organization ownership model is a potential future resolution path requiring a separate explicit owner decision. No transfer is authorized here.

**Approved policy:** Disable private forking unless a specific controlled vendor workflow later requires it. The desired policy is approved, but implementation is blocked by the repository ownership model.

**Exact owner selection:**

- `OWNER DECISION: DISABLE PRIVATE FORKING`
- `CONTROLLED WORKFLOW EXCEPTION: OWNER VALUE REQUIRED ONLY IF A FUTURE EXCEPTION IS REQUESTED`
- `IMPLEMENTATION STATE: OWNER APPROVED — BLOCKED BY REPOSITORY OWNERSHIP MODEL`

### D2 - Vendor-Controlled Git Commit Identity

**Current status:** `OWNER APPROVED — IMPLEMENTED`.

The owner supplied and approved the exact identity values `ALOT <chriltola.khmer@gmail.com>`. Repository-local Git configuration was updated for future commits. Existing history was not rewritten or amended; the pre-change latest commit remains unsigned (`N`).

**Approved policy:** Use one stable vendor-controlled identity for future governance and release commits. Use repository-local Git configuration. The same identity is approved for release and governance commits.

**Tradeoffs:** a stable vendor identity improves accountability and release traceability. The owner-approved vendor-controlled mailbox supports continuity outside GitHub and must be monitored and controlled.

**Exact owner-input fields:**

- `OFFICIAL GIT AUTHOR NAME: ALOT`
- `OFFICIAL GIT AUTHOR EMAIL: chriltola.khmer@gmail.com`
- `GITHUB NOREPLY EMAIL PREFERRED: NO — owner-supplied mailbox is approved`
- `USE SAME IDENTITY FOR RELEASE / GOVERNANCE COMMITS: YES`
- `GIT CONFIGURATION SCOPE: REPOSITORY-LOCAL`

The authorized repository-local configuration is implemented. No global Git configuration was changed.

### D3 - Commit-Signing Mechanism

**Current status:** `OWNER APPROVED — IMPLEMENTED`. SSH signing is configured repository-locally and the new Phase 5.6 governance commit is SSH signed. The public signing key is registered with GitHub; the private key remains outside the repository.

| Option | Operational complexity | GitHub verification compatibility | Custody and recovery | Machine requirements |
| --- | --- | --- | --- | --- |
| SSH commit signing | Low to moderate after setup; familiar to operators already using SSH | Supported by GitHub when the signing key is added and associated correctly | Requires protected SSH signing-key custody, inventory, revocation, backup, and replacement procedures | Git, OpenSSH, and a managed signing key on each signing machine |
| GPG commit signing | Moderate to high; more components and lifecycle administration | Supported by GitHub with an uploaded/associated public key and matching identity | Requires protected private-key custody, passphrase handling, expiry, revocation, backup, and recovery procedures | Git, GnuPG, keyring management, and a managed GPG key |
| Platform-verified mechanism | Potentially low for supported web/platform flows | Depends on the supported GitHub account, plan, and workflow; compatibility must be verified before adoption | Delegates more custody and recovery to the platform; account recovery and access policy become critical | Supported GitHub account/workflow and platform capability |

**Approved policy:** SSH is the selected signing mechanism and is mandatory for official vendor governance/release history. `ALOT` owns the signing key, custody and recovery, rotation, and revocation decisions. The Ed25519 public key fingerprint is `SHA256:GwFTsR04jnXRbLiRnaEhjiGEgnx36rtuWmxFubZHXyo`; the private key is not stored in the repository.

**Exact owner selection:**

- `SIGNING MECHANISM: SSH`
- `MANDATORY FOR OFFICIAL HISTORY: YES`
- `SIGNING IDENTITY / KEY OWNER: ALOT`
- `KEY CUSTODY LOCATION AND RECOVERY OWNER: ALOT`
- `ROTATION / REVOCATION PROCESS OWNER: ALOT`

Repository-local signing configuration: `gpg.format=ssh`, `user.signingkey` points to the dedicated Ed25519 private-key path outside the repository, and `commit.gpgSign=true`.

Rotation and revocation policy: `ALOT` owns rotation and revocation decisions. If compromise is suspected, remove the GitHub public signing key promptly, generate and register a replacement before further official governance/release commits, retire obsolete public keys, keep replacement private keys outside the repository, and do not rewrite previously published history solely because a key was rotated or revoked.

### D4 - Security Contact

**Current status:** `OWNER APPROVED — IMPLEMENTED`.

The approved value is vendor-controlled, monitored, appropriate for private vulnerability reports, not a personal secret, and safe to publish wherever [SECURITY.md](../../../SECURITY.md) requires it. It supports a private intake process and does not require reporters to disclose sensitive material publicly.

**Approved policy:** Use a monitored, vendor-controlled security reporting mailbox that supports private vulnerability intake and has continuity if personnel change. The current approved operational value is `chriltola.khmer@gmail.com`, and the owner-approved shared-mailbox exception applies to both security and commercial/evaluation intake.

**Exact owner-input field:**

- `SECURITY CONTACT: chriltola.khmer@gmail.com`
- `MONITORING / ESCALATION OWNER: ALOT`
- `OWNER-APPROVED SHARED-MAILBOX EXCEPTION: YES`

### D5 - Commercial Contact

**Current status:** `OWNER APPROVED — IMPLEMENTED`.

The approved value is vendor-controlled, monitored, suitable for evaluation and commercial requests, and appropriate for publication in customer-facing documentation. It supports continuity if personnel change and does not expose a personal secret.

**Approved policy:** Use a monitored, vendor-controlled commercial/evaluation mailbox that supports evaluation and commercial intake with continuity if personnel change. The current approved operational value is `chriltola.khmer@gmail.com`, and the owner-approved shared-mailbox exception applies to both security and commercial/evaluation intake.

**Exact owner-input field:**

- `COMMERCIAL CONTACT: chriltola.khmer@gmail.com`
- `EVALUATION REQUEST MONITORING OWNER: ALOT`
- `OWNER-APPROVED SHARED-MAILBOX EXCEPTION: YES`

### D6 - Repository Administrator Assignments

**Current status:** `OWNER APPROVED — IMPLEMENTED`.

| Role | Responsibility | Least-privilege boundary | Owner assignment |
| --- | --- | --- | --- |
| Repository owner / primary administrator | Final repository and business authority | May approve governance and ownership decisions; actions must be recorded | `ALOT` |
| Backup administrator | Continuity for approved administrative operations | May apply explicitly approved settings; no independent business authority unless separately granted | `NOT DESIGNATED` |
| Actions/settings operator | Applies approved Actions and repository settings | Only the approved settings scope; no authority to change business policy | `ALOT` |
| Branch protection/ruleset operator | Applies approved branch controls where platform supports them | Only approved branch/ruleset scope; no visibility or ownership authority | `ALOT` |
| Collaborator/access operator | Manages approved collaborator access | Least privilege, recorded business reason, timely offboarding; no unapproved access grants | `ALOT` |

**Approved policy:** The repository owner remains the primary authority. Only explicitly authorized administrators may alter Actions/settings, branch controls, or collaborators. Least privilege applies, and separation of approval and execution is required where practical. A backup administrator is not designated. The approved governance identity is `ALOT`; the repository account remains the existing `chriltolakhmer-glitch` GitHub owner account, and no permission change is authorized by this record.

**Exact owner-input fields:**

- `PRIMARY REPOSITORY ADMINISTRATOR: ALOT`
- `BACKUP ADMINISTRATOR: NOT DESIGNATED`
- `ACTIONS / REPOSITORY SETTINGS OPERATOR: ALOT`
- `BRANCH PROTECTION / RULESETS OPERATOR: ALOT`
- `COLLABORATOR / ACCESS OPERATOR: ALOT`
- `SEPARATION-OF-DUTIES RULE: REQUIRED WHERE PRACTICAL`

No administrator, collaborator, or role assignment is changed by this packet; the record documents the approved governance identity and responsibility boundaries only.

### D7 - Security Response Owner

**Current status:** `OWNER APPROVED — IMPLEMENTED`.

The Security Response Owner is responsible for receiving or coordinating vulnerability reports, acknowledging intake, triaging severity, assigning remediation, coordinating disclosure timing, preserving confidential handling, and closing the incident with evidence. The role requires authority to coordinate security response, request engineering remediation, approve security-release handling, and escalate unresolved risk to the repository owner. It does not automatically grant repository administration or source-write access.

**Approved policy:** `ALOT` is the approved Security Response Owner. The Security Response backup remains `NOT DESIGNATED`; repository admin access is required for the role; the security-release approval authority remains `ALOT`. This record does not change GitHub permissions or imply that this role independently authorizes a release. Actual tags, GitHub Releases, artifacts, or deployment still require separate release authorization.

**Exact owner-input fields:**

- `SECURITY RESPONSE OWNER: ALOT`
- `SECURITY RESPONSE BACKUP: NOT DESIGNATED`
- `REPOSITORY ADMIN ACCESS REQUIRED: YES`
- `SECURITY RELEASE APPROVAL AUTHORITY: ALOT`

## 4. Platform-Blocked Controls

These controls remain outside the owner-value decision list because filling in names, contacts, or identity values cannot resolve them under the current ownership model:

- enforced private-repository branch protection
- required CI enforcement
- pull-request enforcement
- approving-review enforcement
- force-push prohibition
- branch-deletion restriction

Classification for each remains:

`BLOCKED — PLATFORM / OWNERSHIP MODEL LIMITATION`

The repository must remain private. Making it public is not a workaround and is not recommended or authorized. Moving to an appropriate GitHub Team or Enterprise organization account is only a future option requiring a separate owner decision; this packet does not authorize a transfer or settings change.

## 5. Risks and Tradeoffs

- D6 and D7 are implemented with the approved `ALOT` governance assignments, so the named-administration and response-owner continuity gap is resolved.
- D4 and D5 are implemented with the approved shared-mailbox exception, so vulnerability intake and commercial/evaluation requests are operationally resolved.
- Allowed forking increases private source-copy and access-cleanup risk, while disabling it may impede a controlled vendor workflow.
- Platform-blocked branch controls continue to leave enforcement unavailable even if owner values are supplied.
- The repository remains private and proprietary; no public workaround is authorized.

## 6. Explicit Non-Authorizations

This packet does not authorize or perform:

- implementing any recorded owner decision or changing any GitHub repository setting beyond the completed D3 public-key registration
- changing Git identity or rewriting history
- generating additional SSH, GPG, or other signing keys
- enabling any signing mechanism other than the completed repository-local SSH commit signing
- changing branch protection or rulesets
- changing repository visibility or ownership
- enabling or disabling forking
- changing collaborators or assigning administrators
- inventing or publishing contacts
- creating tags or GitHub Releases
- publishing artifacts or deploying
- issuing customer licenses
- changing source code, tests, CI, or runtime configuration

## 7. Next Steps After Owner Decisions

1. Preserve the recorded policy decisions and supply unresolved exact values in a separately authorized task.
2. Obtain an authorized administrative path before attempting any GitHub setting change.
3. Apply only explicitly approved identity, signing, contact, role, or setting changes in a separately authorized task.
4. Re-verify hosted CI, repository privacy, remote governance state, and secret boundaries after any authorized implementation.
5. Keep Phase 5.6 `IMPLEMENTATION IN PROGRESS` until the platform-blocked controls and remaining owner-value items are resolved and verified.

## 8. Packet Status

`OWNER POLICY DECISIONS RECORDED — IMPLEMENTATION PENDING FOR ALL OPERATIONAL ACTIONS`

This packet does not mark Phase 5.6 complete and does not mark Phase 5 complete.
