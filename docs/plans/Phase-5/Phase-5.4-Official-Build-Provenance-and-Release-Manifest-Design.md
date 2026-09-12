# Phase 5.4 - Official Build Provenance and Release-Manifest Design

> DESIGN DRAFT - REVIEW REQUIRED

Status: PHASE 5.4 — OWNER APPROVED. Documentation/governance approval recorded on 2026-09-12. All Phase 5.4 documentation completion criteria are satisfied. This remains a design record, not a final legal agreement or implementation authorization. Same-host clean publish repeatability is verified; cross-host and cross-environment reproducibility are not yet verified. Open decisions remain unresolved, signing remains deferred and unimplemented, and SBOM remains future governance work. No real release, manifest artifact, package, signing operation, or deployment was performed or authorized by this approval.

## 1. Purpose

Official vendor builds must be distinguishable from arbitrary source builds. This phase defines the provenance, release-manifest, verification, and record-retention requirements that would support that distinction.

The phase defines a controlled design for identifying a build by its product version, source revision, build context, CI evidence, artifact digest, and release operator record. It does not yet implement signing or release automation. Official status is a governance designation and is not cryptographically proven unless an authenticated signing mechanism is later approved and implemented.

## 2. Scope

This design covers:

- build identity and source revision
- release manifest schema and versioning
- artifact filenames, sizes, and SHA-256 checksums
- CI evidence and release-operator records
- product version, release channel, and build timestamp
- official-build and unofficial/source-build terminology
- future artifact and manifest authentication options
- operator and customer verification
- release-record retention and controlled custody
- reproducibility and future SBOM governance

The design is compatible with the existing proprietary, source-available, private/controlled distribution direction and the separation between commercial permission and technical entitlement.

## 3. Non-Goals

This phase does not include:

- production code signing
- release certificates or private signing-key storage
- GitHub Release publication
- automated release-pipeline changes
- a customer portal
- online activation or revocation
- remote attestation
- DRM or machine binding
- public-repository conversion
- licensing runtime changes
- customer-license generation or packaging
- final legal terms

No production values, credentials, secrets, licenses, or signing material are created by this phase.

## 4. Definitions

### Official vendor build

An artifact produced under a vendor-approved release process from a specific repository revision, with recorded provenance and release metadata. The designation depends on the vendor-controlled process and evidence described here. It is not a claim of cryptographic publisher authentication in the unsigned-manifest model.

### Unofficial / source build

Any build produced outside the vendor-controlled release process, including a local developer build, customer rebuild, fork build, or other derivative build. Unofficial does not necessarily mean malicious; it means that official vendor provenance has not been established by the approved process.

### Integrity and authenticity

A checksum demonstrates that an artifact matches the expected bytes represented by a trusted manifest. It does not independently prove who published the manifest or artifact. Publisher authentication requires a separately authenticated manifest, artifact signature, code-signing mechanism, or trusted distribution channel.

## 5. Build Identity

The minimum proposed build identity is:

| Field | Requiredness | Description |
| --- | --- | --- |
| `ProductName` | Required | Product name, currently LabAuthServer. |
| `ProductVersion` | Required | Product release version selected for the build. |
| `GitCommitSha` | Required | Full immutable source commit SHA. |
| `GitBranch` or source reference | Required | Branch, tag, or other source reference used to select the revision. The commit SHA remains authoritative. |
| `BuildTimestampUtc` | Required | UTC timestamp recorded for the build or release event. |
| `BuildConfiguration` | Required | Configuration such as `Release`. |
| `TargetFramework` | Required | Target framework used for the artifact. |
| `CIProvider` | Required for CI builds | CI service that produced or recorded the build. |
| `CIRunId` | Required for CI builds | Immutable provider run identifier. |
| `CIRunNumber` | Optional | Human-facing CI run number where the provider supplies one. |
| `Repository` | Required | Vendor-controlled repository identifier. |
| `ReleaseChannel` | Required for release records | Internal, Evaluation, or Production candidate channel. |
| `BuildOperator` / `ReleaseOperator` reference | Required for approved releases | Controlled operator or record reference; do not put credentials or secrets in the manifest. |

A local source build may lack CI fields and may identify itself as unofficial. No production values are invented by this design. The exact repository identifier, operator references, and release values are supplied only during an approved release process.

The full commit SHA is the authoritative source identity. Branch names, tags, operator references, and notes are supporting metadata and must not override the immutable commit or be treated as proof by themselves. Free-form fields should contain only non-sensitive values needed for provenance and support.

## 6. Release Manifest V1

The proposed machine-readable format is JSON. The conceptual name is **LabAuthServer Release Manifest V1**. The manifest is a provenance record, not a license and not a signing container.

### Proposed schema

```json
{
  "manifestVersion": 1,
  "product": "LabAuthServer",
  "productVersion": "<PRODUCT_VERSION>",
  "releaseChannel": "<INTERNAL|EVALUATION|PRODUCTION>",
  "source": {
    "repository": "<VENDOR_REPOSITORY_IDENTIFIER>",
    "commitSha": "<FULL_GIT_COMMIT_SHA>",
    "branch": "<SOURCE_REFERENCE>"
  },
  "build": {
    "timestampUtc": "<UTC_TIMESTAMP>",
    "configuration": "Release",
    "targetFramework": "<TARGET_FRAMEWORK>",
    "ciProvider": "<CI_PROVIDER>",
    "ciRunId": "<CI_RUN_ID>",
    "ciRunNumber": "<CI_RUN_NUMBER>"
  },
  "artifacts": [
    {
      "fileName": "<ARTIFACT_FILE_NAME>",
      "sizeBytes": 0,
      "sha256": "<LOWERCASE_HEX_SHA256>"
    }
  ],
  "release": {
    "releaseId": "<RELEASE_ID>",
    "releaseOperator": "<CONTROLLED_OPERATOR_REFERENCE>",
    "createdAtUtc": "<UTC_TIMESTAMP>"
  },
  "licensing": {
    "licenseFormatVersion": "<LICENSE_FORMAT_VERSION>",
    "supportedEditions": ["<SUPPORTED_EDITIONS>"],
    "notes": "<NON-SENSITIVE_NOTES>"
  },
  "provenance": {
    "status": "<PROPOSED_STATUS>",
    "verificationNotes": "<NON-SENSITIVE_VERIFICATION_NOTES>"
  }
}
```

The angle-bracket values are placeholders, not production values. A manifest must not contain private keys, passwords, tokens, credentials, customer licenses, or customer secrets. `artifacts` records exact release artifacts. If the manifest itself is distributed as an artifact, its digest is recorded in the external release register or a parent record rather than recursively including its own digest.

## 7. Manifest Versioning Rules

- `manifestVersion` is `1` for this proposed schema.
- Future incompatible schema changes increment the manifest version.
- Unknown required fields must cause validation failure in any future tooling that claims to validate that manifest version.
- Additive optional fields may be introduced only under an explicitly documented compatibility policy.
- Old manifests remain interpretable according to their declared version.
- This document defines the rules; no manifest-validation tooling exists yet.

## 8. Artifact Checksum Design

SHA-256 is the initial integrity checksum. Each artifact record contains its exact filename, exact byte length, and digest.

The canonical digest representation is **lowercase hexadecimal**, with exactly 64 hexadecimal characters and no prefix, whitespace, or separators. Any future tooling should reject a digest that does not use this representation.

A checksum comparison proves artifact integrity against the expected manifest. It does not independently prove vendor identity. The manifest and its delivery channel must therefore be trusted in this initial unsigned model.

### Artifact filename semantics

`fileName` is a normalized relative path within the approved release artifact set, using `/` as the separator. This permits deterministic identification when different artifacts would otherwise share a base filename. It must not be an absolute path, contain a drive letter, contain `..` traversal segments, or include an environment-specific local filesystem path. The recorded path, byte length, and digest must describe the exact artifact delivered.

## 9. Release Artifact Scope

An official release record may cover:

- application publish archive
- deployment package
- release notes when distributed as a release artifact
- the release manifest, with its digest recorded without a self-referential entry
- an optional SBOM in a later approved phase

Phase 5.4 does not create or package real release artifacts. Customer licenses are never included in generic official release artifacts. Customer-specific license delivery remains a separate controlled operational process.

## 10. Manifest Authentication Options

Authentication is intentionally deferred. The options are:

| Option | Trust property | Operational burden | Key-management dependency | Portability | Residual risk |
| --- | --- | --- | --- | --- | --- |
| A. Unsigned manifest through a trusted vendor channel | Integrity and provenance depend on channel and operator trust. | Lowest. | No signing-key dependency, but channel access must be controlled. | Highest. | Channel compromise, manifest replacement, and impersonation remain possible. |
| B. Detached signature over the manifest | Authenticates the manifest to holders of the trusted verification key. | Moderate signing, distribution, rotation, and verification work. | Requires protected private signing keys and a public verification-key process. | Good across platforms. | Key compromise, verifier trust errors, and unsigned artifact substitution remain risks unless artifact hashes are checked. |
| C. Platform-specific code/artifact signing | Can authenticate artifacts to platform-native verification systems and improve platform trust signals. | High across platforms, certificate renewals, and platform procedures. | Requires certificates, private keys, secure custody, and possibly platform accounts. | Lower across heterogeneous platforms. | Account, certificate, build, and platform trust-chain compromise remain possible. |
| D. Signed manifest and signed artifacts | Provides layered manifest provenance and artifact authentication. | Highest. | Requires coordinated key, certificate, rotation, and incident-response governance. | Varies by signing format and target platform. | More complex operations and residual key, CI, distribution, and endpoint compromise risks. |

**Provisional direction:** begin with a deterministic manifest, SHA-256, and controlled vendor release process. Evaluate cryptographic manifest or artifact signing only in a later explicitly approved subphase. No signing is implemented here.

## 11. Build Metadata Embedding

Official builds could expose non-secret metadata such as product version, source commit SHA, release identifier, and build timestamp at runtime or through a diagnostic endpoint.

Benefits include easier support diagnostics, faster artifact-to-source tracing, and clearer customer verification. Risks include exposing repository or operational details, creating stale metadata if packaging is incorrect, and increasing the need to keep embedded values consistent with the manifest.

This phase recommends evaluating metadata embedding as a later implementation decision. No source code or runtime behavior changes are made here.

## 12. Release Identifier Design

A release identifier should be human-readable, unique enough for vendor operations, traceable to the source revision, non-secret, and stable after release.

A candidate concept is `LAS-YYYYMMDD-<version>-<shortsha>`. The exact format remains an owner decision. A release identifier must not be reused for a different source revision, artifact set, or approved release record.

No real release identifier is created by this phase.

## 13. Release Channels

Candidate channels are:

- **Internal**: vendor-controlled testing or release-candidate evaluation.
- **Evaluation**: controlled non-production evaluation distribution.
- **Production**: a candidate for authorized commercial production distribution.

A channel label does not grant legal or commercial permission. A Production-labeled artifact still requires applicable commercial authorization and technical licensing. Evaluation does not automatically mean that a user has legal permission to modify, redistribute, or deploy the software in production. Channel taxonomy and use restrictions remain subject to owner and legal review.

## 14. External Release Register

The vendor should maintain an external, vendor-controlled release register. It is not an application database and is not implemented by this phase.

Recommended fields:

- Release ID
- Product
- Product Version
- Release Channel
- Git Commit SHA
- CI Run ID
- Build Timestamp
- Manifest SHA-256
- Artifact names
- Artifact SHA-256 values
- Release Operator
- Approval Status
- Created At
- Notes

The register must not store production private keys, signing secrets, passwords, credentials, or customer licenses. Access and retention should follow the vendor's controlled operational policy.

## 15. Build / Release Approval Flow

| Stage | Actor | Input | Output | Approval or evidence |
| --- | --- | --- | --- | --- |
| Source revision selected | Release Operator | Approved branch, tag, or commit candidate | Candidate source revision | Revision and selection record |
| CI build and tests | CI / Engineering Owner | Candidate revision and approved build settings | Build/test result | Successful CI run reference |
| Release candidate identified | Release Operator | Successful build/test result | Candidate release identity | Operator records source and channel |
| Artifact hashes calculated | Release Operator or controlled tooling | Candidate artifacts | Filename, byte length, and SHA-256 values | Hash calculation evidence |
| Manifest generated | Release Operator | Build identity and artifact records | Manifest V1 candidate | Schema and field review |
| Release review | Approver(s) | Candidate artifacts, manifest, CI evidence | Approved or rejected release | Review and approval record |
| Official release record created | Release Operator | Approved manifest and evidence | External register entry | Release ID, operator, and creation timestamp |
| Controlled distribution | Distribution Operator | Approved artifact set and manifest | Recipient delivery | Approved channel and delivery record |

This is a governance flow, not an automated pipeline. Separation of duties may be required for production releases; the exact approval roles remain an owner decision.

## 16. CI Boundary

The current CI workflow is build/test only: it checks out the repository, installs the configured .NET SDK, restores, builds Release, provisions disposable SQL audit test infrastructure, and runs tests. It does not publish, package, generate release manifests, calculate release checksums, sign artifacts, deploy, or hold production signing material.

A future release-oriented CI design may eventually produce a deterministic artifact, calculate hashes, generate a manifest, and archive evidence. Those actions require separate approval and design. Phase 5.4 does not modify `.github/workflows/ci.yml`, add secrets, or move production signing keys into general CI.

## 17. Reproducibility

The current reproducibility status is **VERIFIED — SAME-HOST CLEAN PUBLISH REPEATABILITY; CROSS-HOST REPRODUCIBILITY NOT YET VERIFIED**.

On 2026-09-12, two detached, clean temporary Git worktrees on the same host were checked out independently at exact commit `e6f94579022c741ce09f3fde37f8631bcd073533`. The environment was Windows `10.0.20348`, `win-x64`, x64 architecture, with .NET SDK `10.0.401` selected under the repository's `global.json` request for `10.0.400` and `rollForward: latestPatch`. The .NET host/runtime version was `10.0.12`.

Each worktree independently ran:

```powershell
dotnet restore .\LabAuthServer.slnx

dotnet publish `
  .\src\LabAuthServer.Api\LabAuthServer.Api.csproj `
  -c Release `
  --no-restore `
  -p:PublishProfile=FolderProfile `
  -o .\Build\Release
```

The explicit output option selected the profile's documented worktree-local `Build/Release` candidate artifact directory without modifying the existing profile. Both operations used target framework `net10.0`, configuration `Release`, and `FolderProfile`; neither worktree reused the other's `bin`, `obj`, restore outputs, or publish output.

Every regular file under each `Build/Release` directory was enumerated recursively. Paths were normalized relative to that directory with `/` separators and sorted ordinally. For each path, the comparison recorded exact byte size and a lowercase hexadecimal SHA-256 digest. Each publish contained 52 files. The path sets, all byte sizes, and all SHA-256 values matched; there were zero differing paths.

As an additional check, each sorted record set was encoded as UTF-8 without BOM using LF-terminated `<sha256> <sizeBytes> <normalized/path>` records. Both directory fingerprints were `7349b8de3e620bf0ab5f2e76c3598e1756a17813a1037c23ae106d4832fb548e`.

This evidence establishes repeatability only for separate clean restore/build/publish operations from the same exact Git SHA on the same host, OS, SDK, and toolchain. **CROSS-HOST / CROSS-ENVIRONMENT REPRODUCIBILITY — NOT YET VERIFIED.** It does not establish cross-machine, cross-OS, cross-SDK, or universal reproducibility.

Future cross-environment reproducibility verification should consider:

- deterministic build metadata and timestamp handling
- package dependency pinning and lock policy
- SDK and toolchain version pinning
- operating-system and environment influence
- archive ordering, compression, and file metadata
- generated files and embedded source/build values
- documented comparison and exception handling

A recorded build timestamp is useful provenance but can itself prevent byte-for-byte identity if embedded into artifacts. A future release design should distinguish the timestamp used in the manifest from reproducible artifact inputs where necessary.

## 18. SBOM / Dependency Provenance

An SBOM is **RECOMMENDED FOR FUTURE RELEASE GOVERNANCE**. SPDX or CycloneDX are reasonable later format options. An SBOM would improve dependency visibility, customer review, vulnerability response, and release-record completeness.

No SBOM tooling is added in Phase 5.4. The required format, generation point, review process, and retention policy remain deferred to a separately approved implementation or governance subphase.

## 19. Release Verification Procedure

Under the initial unsigned-manifest model, an operator or customer should:

1. Obtain the artifact and manifest through an approved vendor channel.
2. Verify the expected release identifier.
3. Verify the product and product version.
4. Calculate the artifact's SHA-256 digest and exact byte length.
5. Compare both values with the manifest.
6. Verify the commit and release record through vendor-controlled evidence.
7. Verify licensing and commercial authorization separately.

Without authenticated manifest signing, this procedure assumes that the manifest came from a trusted source and was not replaced. If both the artifact and unsigned manifest come from the same compromised or untrusted source, matching hashes establish only consistency with that source; they do not establish vendor authenticity, publisher identity, or legal permission.

## 20. Tampering / Threat Model

| Threat | Impact | Mitigation | Residual risk |
| --- | --- | --- | --- |
| Artifact modified after build | Customer receives altered or corrupted bytes. | Record exact size and SHA-256; verify before use; use controlled distribution. | An attacker who replaces both artifact and trusted manifest may evade unsigned checks. |
| Manifest modified | False provenance or checksum presented to verifier. | Restrict manifest storage and distribution; retain external register evidence; consider later detached signing. | No cryptographic manifest authentication in this phase. |
| Unofficial build represented as vendor release | Customer may trust an unapproved or altered build. | Require source SHA, CI evidence, release ID, operator record, and controlled release register. | Social engineering, record compromise, and unsigned-channel impersonation remain possible. |
| GitHub or release-account compromise | Unauthorized source or release evidence may be changed or distributed. | Least privilege, review, protected branches/accounts, audit logs, and incident response. | Account or provider compromise may bypass repository controls. |
| CI compromise | Malicious artifact may be produced from a legitimate revision. | Review CI changes, restrict permissions, preserve evidence, and separately approve release automation. | Current build/test CI does not by itself prove artifact integrity or build purity. |
| Release-operator mistake | Wrong revision, version, hash, channel, or artifact is recorded. | Two-person review where appropriate, schema checks, and reconciliation against CI evidence. | Human error can survive incomplete review. |
| Source revision mismatch | Manifest does not describe the bytes that were built. | Record full commit SHA, CI run, source reference, and compare build evidence. | Compromised or inaccurate build metadata can mislead an unsigned process. |
| Stale manifest or artifact/manifest mix-up | A valid artifact is paired with metadata for another release, or an old manifest describes a different artifact set. | Bind the release ID, product version, source commit, artifact path, size, and digest together; verify the external register and approval status. | An unsigned channel may still deliver a stale or mismatched pair. |
| Rollback to an older vulnerable release | A recipient is directed to an authentic but outdated artifact with known defects. | Record immutable release IDs and approval status; document supported versions and require an explicit release-selection check. | The initial manifest model does not provide automated rollback prevention or vulnerability policy enforcement. |

This model reduces operational ambiguity but does not overclaim cryptographic security.

## 21. Open Decisions and Placeholders

| Decision | Status | Placeholder / next question |
| --- | --- | --- |
| Final release identifier format | OWNER APPROVED | `LAS-vMAJOR.MINOR.PATCH-<shortsha>`. |
| Final release channel taxonomy | OWNER APPROVED | `Internal / Evaluation / Production`, frozen for the current Phase 5 release-governance model. A channel label does not itself grant legal, commercial, evaluation, or production permission. |
| Manifest storage location | OWNER APPROVED — POLICY | Vendor-controlled private storage; access limited to authorized release operators; immutable or append-preserving history where practical; manifest associated with the exact release ID, Git SHA, and artifact hashes; no private keys, credentials, or customer licenses; customer-facing copies only through the approved distribution process. Exact provider/location: `TO BE SELECTED BEFORE FIRST REAL RELEASE`. |
| Release register storage | OWNER APPROVED — POLICY | Private/vendor-controlled; append-preserving; access-controlled; backed up; auditable; separate from the application database; no private keys, credentials, or customer licenses. Exact product/location: `OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE`. |
| Future manifest signing | DEFERRED | Decide whether and when detached manifest signatures are approved. |
| Future artifact code signing | DEFERRED | Decide platform scope, certificates, custody, and operations. |
| SBOM requirement | DEFERRED | Decide whether the future recommendation becomes mandatory. |
| Runtime build metadata | DEFERRED | Decide whether non-secret provenance is embedded in binaries or diagnostics. |
| Release-record retention period | OWNER APPROVED — OPERATIONAL POLICY | Retain for the supported lifetime and indefinitely thereafter until superseded by an approved legal/business retention policy. Professional legal/contractual retention review remains pending. |
| Release approval separation of duties | OWNER APPROVED | Release Operator `ALOT`; Release Approval Authority `ALOT` initially; Distribution Operator `ALOT` initially; Security Response Owner `ALOT`; separation of duties required where practical; second-person review when another authorized reviewer exists; absence must be recorded. |

No business choice is silently finalized by this design.

## 22. Phase 5.4 Completion Checklist

- [x] Official build definition documented.
- [x] Build identity defined.
- [x] Release Manifest V1 designed.
- [x] Artifact checksum format defined.
- [x] Release artifact scope defined.
- [x] Future authentication options evaluated.
- [x] Runtime metadata option evaluated.
- [x] Release ID design evaluated.
- [x] Release channels evaluated.
- [x] Release register defined.
- [x] Approval flow defined.
- [x] CI boundary preserved.
- [x] Same-host clean publish repeatability verified; cross-host reproducibility remains not yet verified.
- [x] SBOM future recommendation recorded.
- [x] Verification procedure documented.
- [x] Threat model completed.
- [x] Open decisions recorded.
- [x] No signing implementation added.
- [x] No source, runtime, or CI changes introduced.

## 23. Security and Legal Boundary

This document introduces no private keys, passwords, tokens, credentials, production signing material, customer licenses, or customer secrets. It does not alter licensing runtime behavior or assert final legal terms. Commercial permission, technical entitlement, and release-channel labels remain separate concerns and require the applicable vendor and professional legal review.

## 24. Current Status

PHASE 5.4 — P54-D1 THROUGH P54-D6 OWNER APPROVED — 2026-09-12

The project owner approved the completed Phase 5.4 documentation on 2026-09-12, and on 2026-09-12 also approved the remaining operating-policy decisions P54-D1 through P54-D6 as recorded in section 21 and in [Phase-5.4-Remaining-Owner-Decision-Packet.md](Phase-5.4-Remaining-Owner-Decision-Packet.md).

- P54-D1 through P54-D6 are `OWNER APPROVED`.
- Same-host clean publish repeatability remains `VERIFIED — SAME-HOST CLEAN PUBLISH REPEATABILITY`.
- Cross-host and cross-environment reproducibility remain `NOT YET VERIFIED`.
- Manifest storage and release-register storage policies are approved; the exact provider/product and location values remain future pre-release operational prerequisites (`TO BE SELECTED BEFORE FIRST REAL RELEASE` and `OWNER VALUE REQUIRED BEFORE FIRST REAL RELEASE`).
- Legal/contractual retention remains `LEGAL / CONTRACTUAL RETENTION REQUIREMENT — PROFESSIONAL REVIEW PENDING`; the approved retention rule is an operational preservation policy only.
- Manifest signing remains `DEFERRED`.
- Artifact / code signing remains `DEFERRED / NOT IMPLEMENTED`.
- SBOM remains `DEFERRED / FUTURE GOVERNANCE`.
- Runtime build metadata remains `DEFERRED`.
- No real release, Release Manifest V1 artifact, package, signing operation, or deployment was produced or performed, and no release has been authorized. The repository remains private and controlled.

This record does not mark Phase 5 fully complete and does not authorize release publication, customer distribution, production key generation, or implementation changes.
