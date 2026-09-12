# Phase 5.6 - Owner Decision Packet

> DOCUMENTATION-ONLY DECISION PREPARATION — PHASE 5.6 REMAINS IMPLEMENTATION IN PROGRESS

## 1. Purpose

This packet prepares the remaining Phase 5.6 governance decisions for separate owner review. It records current evidence, recommendations, tradeoffs, and blank owner-input fields. It does not approve, implement, or configure any decision.

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
| Git identity | `unknown <Administrator@LAB.LOCAL>` |
| Latest commit signature | `N` (unsigned) |
| Security contact | `<SECURITY_CONTACT>` |
| Commercial contact | `<COMMERCIAL_CONTACT>` |

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

**Current status:** `OWNER POLICY APPROVED — EXACT IDENTITY VALUES STILL REQUIRED`.

Current local and latest-commit evidence remains `unknown <Administrator@LAB.LOCAL>`. No identity was changed and existing history must not be rewritten by this packet.

**Approved policy:** Use one stable vendor-controlled identity for future governance and release commits. Prefer a GitHub-associated noreply address if the owner chooses privacy/platform association. Use repository-local Git configuration. The exact name and email remain unresolved.

**Tradeoffs:** a stable vendor identity improves accountability and release traceability. A GitHub noreply address may reduce personal-address exposure and can support platform association, but its exact value must be supplied and confirmed by the owner. A vendor-controlled mailbox may improve continuity outside GitHub but must be monitored and controlled.

**Exact owner-input fields:**

- `OFFICIAL GIT AUTHOR NAME: OWNER VALUE REQUIRED`
- `OFFICIAL GIT AUTHOR EMAIL: OWNER VALUE REQUIRED`
- `GITHUB NOREPLY EMAIL PREFERRED: YES`
- `USE SAME IDENTITY FOR RELEASE / GOVERNANCE COMMITS: YES`
- `GIT CONFIGURATION SCOPE: REPOSITORY-LOCAL`

No `git config` operation is authorized by this packet.

### D3 - Commit-Signing Mechanism

**Current status:** `OWNER APPROVED — IMPLEMENTATION BLOCKED BY KEY/CUSTODY VALUES`. Current signing remains unresolved; the latest commit is unsigned (`N`).

| Option | Operational complexity | GitHub verification compatibility | Custody and recovery | Machine requirements |
| --- | --- | --- | --- | --- |
| SSH commit signing | Low to moderate after setup; familiar to operators already using SSH | Supported by GitHub when the signing key is added and associated correctly | Requires protected SSH signing-key custody, inventory, revocation, backup, and replacement procedures | Git, OpenSSH, and a managed signing key on each signing machine |
| GPG commit signing | Moderate to high; more components and lifecycle administration | Supported by GitHub with an uploaded/associated public key and matching identity | Requires protected private-key custody, passphrase handling, expiry, revocation, backup, and recovery procedures | Git, GnuPG, keyring management, and a managed GPG key |
| Platform-verified mechanism | Potentially low for supported web/platform flows | Depends on the supported GitHub account, plan, and workflow; compatibility must be verified before adoption | Delegates more custody and recovery to the platform; account recovery and access policy become critical | Supported GitHub account/workflow and platform capability |

**Approved policy:** SSH is the selected signing mechanism and should be mandatory for official vendor governance/release history once implemented. Key custody must remain vendor-controlled, with rotation and revocation procedures documented before production use.

**Exact owner selection:**

- `SIGNING MECHANISM: SSH`
- `MANDATORY FOR OFFICIAL HISTORY: YES`
- `SIGNING IDENTITY / KEY OWNER: OWNER VALUE REQUIRED`
- `KEY CUSTODY LOCATION AND RECOVERY OWNER: OWNER VALUE REQUIRED`
- `ROTATION / REVOCATION PROCESS OWNER: OWNER VALUE REQUIRED`

No keys will be generated and signing will not be configured by this packet.

### D4 - Security Contact

**Current status:** `OWNER POLICY APPROVED — CONTACT VALUE REQUIRED`.

The eventual value must be vendor-controlled, monitored, appropriate for vulnerability reports, not a personal secret, and safe to publish wherever [SECURITY.md](../../../SECURITY.md) requires it. It should support a private intake process and should not require reporters to disclose sensitive material publicly.

**Approved policy:** Use a dedicated, monitored, vendor-controlled security reporting channel that supports private vulnerability reporting, is safe to publish in `SECURITY.md`, and has continuity if personnel change. The exact contact remains unresolved.

**Exact owner-input field:**

- `SECURITY CONTACT: OWNER VALUE REQUIRED`
- `MONITORING / ESCALATION OWNER: OWNER VALUE REQUIRED`

### D5 - Commercial Contact

**Current status:** `OWNER POLICY APPROVED — CONTACT VALUE REQUIRED`.

The eventual value must be vendor-controlled, monitored, suitable for evaluation and commercial requests, and appropriate for publication in customer-facing documentation. It must support continuity if personnel change and must not expose a personal secret.

**Approved policy:** Use a dedicated, monitored, vendor-controlled commercial/evaluation channel separate from security intake where practical. It must be suitable for customer-facing documentation and support evaluation and commercial requests with continuity if personnel change. The exact value remains unresolved.

**Exact owner-input field:**

- `COMMERCIAL CONTACT: OWNER VALUE REQUIRED`
- `EVALUATION REQUEST MONITORING OWNER: OWNER VALUE REQUIRED`

### D6 - Repository Administrator Assignments

**Current status:** `OWNER POLICY APPROVED — EXACT NAMED ASSIGNMENTS STILL REQUIRED WHERE APPLICABLE`.

| Role | Responsibility | Least-privilege boundary | Owner assignment |
| --- | --- | --- | --- |
| Repository owner / primary administrator | Final repository and business authority | May approve governance and ownership decisions; actions must be recorded | `OWNER VALUE REQUIRED` |
| Backup administrator | Continuity for approved administrative operations | May apply explicitly approved settings; no independent business authority unless separately granted | `OWNER VALUE REQUIRED or NOT DESIGNATED` |
| Actions/settings operator | Applies approved Actions and repository settings | Only the approved settings scope; no authority to change business policy | `OWNER VALUE REQUIRED or NOT DESIGNATED` |
| Branch protection/ruleset operator | Applies approved branch controls where platform supports them | Only approved branch/ruleset scope; no visibility or ownership authority | `OWNER VALUE REQUIRED or NOT DESIGNATED` |
| Collaborator/access operator | Manages approved collaborator access | Least privilege, recorded business reason, timely offboarding; no unapproved access grants | `OWNER VALUE REQUIRED or NOT DESIGNATED` |

**Approved policy:** The repository owner remains the primary authority. Only explicitly authorized administrators may alter Actions/settings, branch controls, or collaborators. Least privilege applies, and separation of approval and execution should be used where practical. A backup administrator is optional.

**Exact owner-input fields:**

- `PRIMARY REPOSITORY ADMINISTRATOR: OWNER`
- `BACKUP ADMINISTRATOR: NOT DESIGNATED`
- `WHO MAY ALTER ACTIONS / REPOSITORY SETTINGS: EXPLICITLY AUTHORIZED ADMINISTRATORS ONLY`
- `WHO MAY ALTER BRANCH PROTECTION / RULESETS: EXPLICITLY AUTHORIZED ADMINISTRATORS ONLY`
- `WHO MAY MANAGE COLLABORATORS: EXPLICITLY AUTHORIZED ADMINISTRATORS ONLY`
- `SEPARATION-OF-DUTIES RULE: REQUIRED WHERE PRACTICAL`

No administrator, collaborator, or role assignment will be made by this packet.

### D7 - Security Response Owner

**Current status:** `OWNER POLICY APPROVED — EXACT NAMED IDENTITY STILL REQUIRED WHERE APPLICABLE`.

The Security Response Owner is responsible for receiving or coordinating vulnerability reports, acknowledging intake, triaging severity, assigning remediation, coordinating disclosure timing, preserving confidential handling, and closing the incident with evidence. The role requires authority to coordinate security response, request engineering remediation, approve security-release handling, and escalate unresolved risk to the repository owner. It does not automatically grant repository administration or source-write access.

**Approved policy:** The repository owner will initially act as Security Response Owner unless a dedicated trusted security operator is later assigned. The role has authority to coordinate response, request remediation, approve security-release handling, and escalate unresolved risk. It does not automatically grant GitHub permissions.

**Exact owner-input fields:**

- `SECURITY RESPONSE OWNER: REPOSITORY OWNER — INITIAL POLICY`
- `SECURITY RESPONSE BACKUP: NOT DESIGNATED`
- `REPOSITORY ADMIN ACCESS REQUIRED: YES`
- `SECURITY RELEASE APPROVAL AUTHORITY: REPOSITORY OWNER — INITIAL POLICY`
- If a personal identity is required by repository convention: `OWNER VALUE REQUIRED`

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
- Unresolved named administrator and response-owner identities create continuity and response gaps even though the policy roles are approved.
- Allowed forking increases private source-copy and access-cleanup risk, while disabling it may impede a controlled vendor workflow.
- Platform-blocked branch controls continue to leave enforcement unavailable even if owner values are supplied.
- The repository remains private and proprietary; no public workaround is authorized.

## 6. Explicit Non-Authorizations

This packet does not authorize or perform:

- implementing any recorded owner decision or changing any GitHub repository setting
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

1. Preserve the recorded policy decisions and supply unresolved exact values in a separately authorized task.
2. Obtain an authorized administrative path before attempting any GitHub setting change.
3. Apply only explicitly approved identity, signing, contact, role, or setting changes in a separately authorized task.
4. Re-verify hosted CI, repository privacy, remote governance state, and secret boundaries after any authorized implementation.
5. Keep Phase 5.6 `IMPLEMENTATION IN PROGRESS` until the platform-blocked controls and remaining owner-value items are resolved and verified.

## 8. Packet Status

`OWNER POLICY DECISIONS RECORDED — IMPLEMENTATION PENDING FOR ALL OPERATIONAL ACTIONS`

This packet does not mark Phase 5.6 complete and does not mark Phase 5 complete.
