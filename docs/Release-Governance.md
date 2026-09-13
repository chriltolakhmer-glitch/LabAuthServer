# Release Governance

Effective: **2026-09-13**. Release owner: **ALOT**.

Future LabAuthServer releases use the [Solo Developer Release Checklist](plans/Phase-6/Release-Checklist.md). Its six requirements are the complete release gate:

1. Release build passes.
2. Tests pass.
3. Artifact package exists.
4. SHA256 checksum exists and matches the package.
5. Smoke test passes.
6. Git tag is created at the tested source commit.

ALOT performs the checks and keeps one completed checklist with the package and checksum. No external owner acknowledgement, legal approval, operations approval, rollback committee, artifact custody workflow, separate authorization packet, or second-person review blocks release completion. Release Manifest V1, SBOMs, and release-tag/manifest/artifact signing are optional for this workflow. Existing commit-signing configuration is unchanged.

## Version and artifact identity

Use `vMAJOR.MINOR.PATCH` tags and record the exact source SHA, package filename, checksum, and validation results. Published tags and artifacts are immutable; a correction receives a new version. ALOT may mark a release superseded or withdrawn while retaining its history.

## Unchanged scope

The repository remains private. This governance refactor does not change application behavior, architecture, security controls, commercial/license terms, supported-version policy, or repository visibility. Existing deployment and licensing procedures apply to those activities; they do not add approval gates to the six release checks. No release, tag, publication, deployment, or license issuance is performed by editing these documents.

Supported-version policy remains: current release supported; immediately previous MINOR release receives security/critical fixes where practical; older versions unsupported unless a commercial agreement says otherwise. No fixed calendar support term or LTS designation is established. A commercial license does not automatically grant software-version support entitlement.

## History

The [previous release policy](archive/phase-6-multi-owner/Release-Governance.md) and [Phase 6 multi-owner archive](archive/phase-6-multi-owner/README.md) preserve the old evidence/approval workflow and historical outcomes. The checklist supersedes conflicting release-process requirements in those records and earlier Phase 5 release plans. Historical missing evidence is not retroactively marked complete.
