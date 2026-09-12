# Phase 5.6 - Owner Decision Packet

> DOCUMENTATION-ONLY DECISION PREPARATION — PHASE 5.6 REMAINS IMPLEMENTATION IN PROGRESS

## 1. Purpose

This packet prepares the remaining Phase 5.6 governance decisions for separate owner review. It records current evidence, recommendations, tradeoffs, and blank owner-input fields. It does not approve, implement, or configure any decision.

Phase 5 remains `PHASE 5 IN PROGRESS — PHASE 5.6 REMAINS OPEN`. Phase 5.6 remains `IMPLEMENTATION IN PROGRESS`. The repository remains `PRIVATE`. Remote governance controls remain `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION`.

## 2. Current Verified Baseline

| Item | Current value |
| --- | --- |
| Branch | `main` |
| HEAD before this packet | `99d840bc79037469aa14ed1e8554602c4d1d347f` |
| HEAD commit | `Reconcile Phase 5.6 governance blockers` |
| Hosted validation | `LabAuthServer CI`, run `#19`, run ID `34686664673`, head `99d840bc79037469aa14ed1e8554602c4d1d347f`, `completed` / `success` |
| Repository visibility | `PRIVATE` |
| Phase 5 status | `PHASE 5 IN PROGRESS — PHASE 5.6 REMAINS OPEN` |
| Phase 5.6 status | `IMPLEMENTATION IN PROGRESS` |
| Main governance state | `CONFIGURED BUT NOT ENFORCED — PLATFORM / OWNERSHIP MODEL LIMITATION` |
| Forking | `ALLOWED` (`allow_forking = true`) |
| Git identity | `unknown <Administrator@LAB.LOCAL>` |
| Latest commit signature | `N` (unsigned) |
| Security contact | `<SECURITY_CONTACT>` |
| Commercial contact | `<COMMERCIAL_CONTACT>` |

The hosted validation result above is accepted as externally verified evidence. Local `gh` authentication is not required to re-prove it.

Completed Phase 5.6 control preserved by this packet: immutable GitHub Actions SHA pinning, already hosted-CI validated. The CI workflow remains unchanged.

## 3. Decisions Required

### D1 - Repository Forking

**Current status:** `CONSISTENCY ISSUE — OWNER CONFIRMATION REQUIRED`

**Current observed state:** Forking is `ALLOWED`.

**Existing policy evidence:**

- Phase 5.5 section 5 records forking as `ALLOWED` and recommends disabling it unless a specific controlled vendor workflow requires forks.
- Phase 5.5 section 13 states: `OWNER APPROVED - 2026-09-12. Forking should be disabled unless a specific controlled vendor workflow later requires it.` It also states that the current setting remains `ALLOWED` and disablement is a future operational action.
- Phase 5.6 classifies the item as `OWNER DECISION REQUIRED` and records that no fork setting was changed.

These statements do not safely establish whether the owner intended the Phase 5.5 direction itself to authorize later disablement or whether a separate explicit fork-setting decision is still required. The setting must remain unchanged until clarified.

**Security benefit of disabling unnecessary private forking:** reduces additional private source copies, limits source-access cleanup obligations, and narrows proprietary-source exposure.

**Operational downside:** may block legitimate controlled vendor or organization workflows that depend on forks. Retaining forks is operationally flexible but expands the number of private copies and access-removal obligations.

**Platform / ownership consideration:** the current private personal-account/plan model already limits enforcement of branch controls. Forking is a separate repository setting, but changing it still requires an authorized GitHub administration path. A plan or ownership change must not be inferred from this packet.

**RECOMMENDATION — NOT APPROVED:** Confirm the Phase 5.5 direction and disable unnecessary private forking, unless the owner identifies a specific controlled workflow that requires it. This recommendation does not authorize the setting change.

**Exact owner selection:**

- `OWNER DECISION REQUIRED - FORKING: [ ] DISABLE PRIVATE FORKING  [ ] RETAIN ALLOWED FOR CONTROLLED WORKFLOW`
- If retaining: `CONTROLLED WORKFLOW JUSTIFICATION: OWNER VALUE REQUIRED`
- `OWNER CONFIRMATION OF PHASE 5.5 / PHASE 5.6 INTERPRETATION: OWNER VALUE REQUIRED`

### D2 - Vendor-Controlled Git Commit Identity

**Current status:** `BLOCKED — OWNER VALUE REQUIRED`.

Current local and latest-commit evidence remains `unknown <Administrator@LAB.LOCAL>`. No identity was changed and existing history must not be rewritten by this packet.

**RECOMMENDATION — NOT APPROVED:** select a stable vendor-controlled identity before future official governance or release commits. Use an identity that the owner can maintain and associate with the vendor. Repository evidence does not expose a safe real identity or email option; no address is proposed here.

**Tradeoffs:** a stable vendor identity improves accountability and release traceability. A GitHub noreply address may reduce personal-address exposure and can support platform association, but its exact value must be supplied and confirmed by the owner. A vendor-controlled mailbox may improve continuity outside GitHub but must be monitored and controlled.

**Exact owner-input fields:**

- `OFFICIAL GIT AUTHOR NAME: OWNER VALUE REQUIRED`
- `OFFICIAL GIT AUTHOR EMAIL: OWNER VALUE REQUIRED`
- `GITHUB NOREPLY EMAIL PREFERRED: OWNER DECISION REQUIRED - [ ] YES  [ ] NO  [ ] UNDECIDED`
- `USE SAME IDENTITY FOR RELEASE / GOVERNANCE COMMITS: OWNER DECISION REQUIRED - [ ] YES  [ ] NO`
- `GIT CONFIGURATION SCOPE: OWNER DECISION REQUIRED - [ ] REPOSITORY-LOCAL  [ ] USER/GLOBAL  [ ] OTHER OWNER-SPECIFIED SCOPE`

No `git config` operation is authorized by this packet.

### D3 - Commit-Signing Mechanism

**Current status:** `BLOCKED — SIGNING MECHANISM DECISION REQUIRED`. Current signing remains unresolved; the latest commit is unsigned (`N`).

| Option | Operational complexity | GitHub verification compatibility | Custody and recovery | Machine requirements |
| --- | --- | --- | --- | --- |
| SSH commit signing | Low to moderate after setup; familiar to operators already using SSH | Supported by GitHub when the signing key is added and associated correctly | Requires protected SSH signing-key custody, inventory, revocation, backup, and replacement procedures | Git, OpenSSH, and a managed signing key on each signing machine |
| GPG commit signing | Moderate to high; more components and lifecycle administration | Supported by GitHub with an uploaded/associated public key and matching identity | Requires protected private-key custody, passphrase handling, expiry, revocation, backup, and recovery procedures | Git, GnuPG, keyring management, and a managed GPG key |
| Platform-verified mechanism | Potentially low for supported web/platform flows | Depends on the supported GitHub account, plan, and workflow; compatibility must be verified before adoption | Delegates more custody and recovery to the platform; account recovery and access policy become critical | Supported GitHub account/workflow and platform capability |

**RECOMMENDATION — NOT APPROVED:** SSH commit signing is the most proportionate option in the current repository evidence because Phase 5.5 already documents it as a good-compatibility, low-to-moderate-burden option. This is only a recommendation. GPG or a platform-verified mechanism may be selected if the owner prefers their custody or verification model.

**Exact owner selection:**

- `SIGNING MECHANISM: OWNER DECISION REQUIRED - [ ] SSH  [ ] GPG  [ ] PLATFORM-VERIFIED  [ ] OTHER OWNER-APPROVED MECHANISM`
- `SIGNING IDENTITY / KEY OWNER: OWNER VALUE REQUIRED`
- `KEY CUSTODY LOCATION AND RECOVERY OWNER: OWNER VALUE REQUIRED`
- `ROTATION / REVOCATION PROCESS OWNER: OWNER VALUE REQUIRED`
- `MANDATORY FOR OFFICIAL HISTORY: OWNER DECISION REQUIRED - [ ] YES  [ ] NO`

No keys will be generated and signing will not be configured by this packet.

### D4 - Security Contact

**Current status:** `BLOCKED — OWNER VALUE REQUIRED`.

The eventual value must be vendor-controlled, monitored, appropriate for vulnerability reports, not a personal secret, and safe to publish wherever [SECURITY.md](../../../SECURITY.md) requires it. It should support a private intake process and should not require reporters to disclose sensitive material publicly.

**RECOMMENDATION — NOT APPROVED:** use a monitored vendor-controlled security-reporting channel with documented access continuity and an assigned response owner. This does not supply an address.

**Exact owner-input field:**

- `SECURITY CONTACT: OWNER VALUE REQUIRED`
- `MONITORING / ESCALATION OWNER: OWNER VALUE REQUIRED`

### D5 - Commercial Contact

**Current status:** `BLOCKED — OWNER VALUE REQUIRED`.

The eventual value must be vendor-controlled, monitored, suitable for evaluation and commercial requests, and appropriate for publication in customer-facing documentation. It must support continuity if personnel change and must not expose a personal secret.

**RECOMMENDATION — NOT APPROVED:** use a monitored vendor-controlled commercial/evaluation channel separate from security intake where practical. This does not supply an address.

**Exact owner-input field:**

- `COMMERCIAL CONTACT: OWNER VALUE REQUIRED`
- `EVALUATION REQUEST MONITORING OWNER: OWNER VALUE REQUIRED`

### D6 - Repository Administrator Assignments

**Current status:** `BLOCKED — OWNER VALUE REQUIRED`.

| Role | Responsibility | Least-privilege boundary | Owner assignment |
| --- | --- | --- | --- |
| Repository owner / primary administrator | Final repository and business authority | May approve governance and ownership decisions; actions must be recorded | `OWNER VALUE REQUIRED` |
| Backup administrator | Continuity for approved administrative operations | May apply explicitly approved settings; no independent business authority unless separately granted | `OWNER VALUE REQUIRED or NOT DESIGNATED` |
| Actions/settings operator | Applies approved Actions and repository settings | Only the approved settings scope; no authority to change business policy | `OWNER VALUE REQUIRED or NOT DESIGNATED` |
| Branch protection/ruleset operator | Applies approved branch controls where platform supports them | Only approved branch/ruleset scope; no visibility or ownership authority | `OWNER VALUE REQUIRED or NOT DESIGNATED` |
| Collaborator/access operator | Manages approved collaborator access | Least privilege, recorded business reason, timely offboarding; no unapproved access grants | `OWNER VALUE REQUIRED or NOT DESIGNATED` |

**RECOMMENDATION — NOT APPROVED:** assign only named, vendor-controlled operators with separate approval and execution responsibilities where practical. Do not grant a role merely because it is convenient.

**Exact owner-input fields:**

- `PRIMARY REPOSITORY ADMINISTRATOR: OWNER VALUE REQUIRED`
- `BACKUP ADMINISTRATOR: OWNER VALUE REQUIRED or NOT DESIGNATED`
- `WHO MAY ALTER ACTIONS / REPOSITORY SETTINGS: OWNER VALUE REQUIRED`
- `WHO MAY ALTER BRANCH PROTECTION / RULESETS: OWNER VALUE REQUIRED`
- `WHO MAY MANAGE COLLABORATORS: OWNER VALUE REQUIRED`
- `SEPARATION-OF-DUTIES RULE: OWNER DECISION REQUIRED`

No administrator, collaborator, or role assignment will be made by this packet.

### D7 - Security Response Owner

**Current status:** `BLOCKED — OWNER VALUE REQUIRED`.

The Security Response Owner is responsible for receiving or coordinating vulnerability reports, acknowledging intake, triaging severity, assigning remediation, coordinating disclosure timing, preserving confidential handling, and closing the incident with evidence. The role requires authority to coordinate security response, request engineering remediation, approve security-release handling, and escalate unresolved risk to the repository owner. It does not automatically grant repository administration or source-write access.

**RECOMMENDATION — NOT APPROVED:** assign a named vendor-controlled person or team with monitored intake coverage, escalation authority, continuity coverage, and a documented backup. Do not assign the role automatically.

**Exact owner-input fields:**

- `SECURITY RESPONSE OWNER: OWNER VALUE REQUIRED`
- `SECURITY RESPONSE BACKUP: OWNER VALUE REQUIRED or NOT DESIGNATED`
- `REPOSITORY ADMIN ACCESS REQUIRED: OWNER DECISION REQUIRED - [ ] YES  [ ] NO`
- `SECURITY RELEASE APPROVAL AUTHORITY: OWNER VALUE REQUIRED`

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

- Unresolved identity and signing leave official history with weak attribution and no cryptographic authorship proof.
- Unresolved contacts block dependable vulnerability intake and commercial/evaluation requests.
- Unassigned administrators and Security Response Owner create continuity and response gaps.
- Allowed forking increases private source-copy and access-cleanup risk, while disabling it may impede a controlled vendor workflow.
- Platform-blocked branch controls continue to leave enforcement unavailable even if owner values are supplied.
- The repository remains private and proprietary; no public workaround is authorized.

## 6. Explicit Non-Authorizations

This packet does not authorize or perform:

- any owner decision or GitHub repository setting change
- changing Git identity or rewriting history
- generating or configuring SSH, GPG, or other signing keys
- enabling commit or tag signing
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

1. Record each owner decision and supplied value separately, preserving unresolved fields for decisions not made.
2. Reconcile the D1 fork-policy inconsistency before changing the fork setting.
3. Obtain an authorized administrative path before attempting any GitHub setting change.
4. Apply only explicitly approved identity, signing, contact, role, or setting changes in a separately authorized task.
5. Re-verify hosted CI, repository privacy, remote governance state, and secret boundaries after any authorized implementation.
6. Keep Phase 5.6 `IMPLEMENTATION IN PROGRESS` until the platform-blocked controls and owner-value decisions are resolved and verified.

## 8. Packet Status

`OWNER DECISIONS REQUIRED — NO DECISIONS IMPLEMENTED`

This packet does not mark Phase 5.6 complete and does not mark Phase 5 complete.
