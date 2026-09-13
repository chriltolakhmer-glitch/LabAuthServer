# Solo Developer Release Checklist

Owner: **ALOT**  
Effective: **2026-09-13**  
Status: reusable template; no release is recorded by this blank checklist.

ALOT performs and records these six checks for each release. This is the complete release gate, replacing the Phase 6 multi-owner evidence and authorization workflow. External owner acknowledgements, legal approval, operations approval, a rollback committee, and artifact custody workflows are not release prerequisites. Separate approval packets, reviewer signatures, custody registers, signed tags, SBOMs, and Release Manifest V1 are not required.

## Release record

Copy this checklist for each release and fill in the evidence below. Keep the completed record with the package and checksum; one record is enough.

- Version / tag: `vMAJOR.MINOR.PATCH`
- Source commit SHA:
- Date:
- Owner: **ALOT**
- Package path / filename:
- SHA256 file / digest:
- Smoke-test environment / URL:

## Required checks

- [ ] **Release build passes.** Restore and build the selected source commit in Release configuration; record the result with zero warnings/errors.
- [ ] **Tests pass.** Run the default unit and integration suite in Release configuration using the infrastructure-safe filter below; record pass/fail counts. Additional infrastructure checks follow [Testing](../../Testing.md) when applicable, using explicit disposable or authorized targets. Do not count excluded or skipped infrastructure tests as passing.
- [ ] **Artifact package exists.** Publish the API from the same source commit into a fresh staging directory and create the versioned package. Confirm the package contains the intended Release output and no secrets, private keys, development configuration, or local operational settings. Record its location.
- [ ] **SHA256 checksum exists.** Generate a `.sha256` file from the final package and verify that its digest matches the package bytes. Record the checksum location and digest.
- [ ] **Smoke test passes.** Extract and run that package in ALOT's test environment with external configuration. Confirm HTTPS `GET /api/v1/health` returns `200` with the expected healthy response and anonymous `GET /api/v1/protected` returns `401`. Record the package digest, environment, date, and results. Health proves liveness only; it does not prove AD login or SQL readiness.
- [ ] **Git tag created.** After the preceding checks pass, create `vMAJOR.MINOR.PATCH` at the recorded source commit and verify the tag resolves to that SHA. Record the tag. An annotated tag is sufficient; signing is optional. Published tags and package bytes are immutable; corrections receive a new version.

ALOT marks the release complete when all six checks pass for the same source/package. A failed or unperformed check remains incomplete. No external acknowledgement or additional authorization form is needed.

## Command reference

Run in a dedicated PowerShell session from the selected release checkout. Fill in version, commit, and output paths for the release. Use a fresh staging directory for each package.

```powershell
Remove-Item Env:LABAUTHSERVER_RUN_SQL_TESTS, Env:LABAUTHSERVER_SQL_AUDIT_TEST_CONNECTION, Env:LABAUTHSERVER_RUN_LDAP_ACCEPTANCE -ErrorAction SilentlyContinue
dotnet restore .\LabAuthServer.slnx
dotnet build .\LabAuthServer.slnx -c Release --no-restore --nologo
dotnet test .\LabAuthServer.slnx -c Release --no-build --no-restore -m:1 --filter "Category!=SqlInfrastructure&Category!=LdapAcceptance"
```

Stop if any command fails. Publish the API with `dotnet publish src/LabAuthServer.Api/LabAuthServer.Api.csproj -c Release --no-restore -o <fresh-staging-directory>`, inspect the contents, and package them. The solution build also qualifies the offline LicenseIssuer utility; it is not part of the API package.

For the final package, set `$releasePackage` to its actual path:

```powershell
$releaseHash = (Get-FileHash -LiteralPath $releasePackage -Algorithm SHA256).Hash.ToLowerInvariant()
"$releaseHash  $([IO.Path]::GetFileName($releasePackage))" | Set-Content -LiteralPath "$releasePackage.sha256" -Encoding ascii
$recordedHash = ((Get-Content -LiteralPath "$releasePackage.sha256" -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash -LiteralPath $releasePackage -Algorithm SHA256).Hash.ToLowerInvariant() -ne $recordedHash) { throw 'Package checksum mismatch' }
```

After the smoke test, set `$releaseTag` and `$releaseCommit` to the recorded values:

```powershell
git -c tag.gpgSign=false tag -a $releaseTag $releaseCommit -m "Release $releaseTag"
if ($LASTEXITCODE -ne 0) { throw 'Tag creation failed' }
git rev-parse "$releaseTag^{commit}"
```

Compare the returned SHA with the release record. Do not force-replace an existing tag.

## Scope and history

This workflow governs release qualification and tagging. Repository visibility, commercial/license terms, application security, and deployment procedures retain their existing scope. This documentation change does not itself publish, deploy, issue a customer license, or create a tag.

The [multi-owner archive](../../archive/phase-6-multi-owner/README.md) preserves earlier approvals, evidence gaps, and the stopped preflight. Those historical pending items are superseded as release gates, not retroactively marked complete. Technical limitations remain documented in [Validation Status](../../Validation_Status.md).
