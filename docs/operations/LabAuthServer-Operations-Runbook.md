# LabAuthServer operations runbook

Verified 2026-09-21 (Asia/Bangkok). Solo-developer baseline for the existing v1.0.0 deployment. Procedures below are for later operational use; no restart, authentication test, configuration change or rollback was executed during this handoff. See the [deployment record](../plans/Phase-7/Phase-7-Deployment-Record.md) and [recovery checklist](LabAuthServer-Recovery-Checklist.md).

## Verified deployment

| Item | Actual value |
| --- | --- |
| Server | `DC01.lab.local`, domain controller, Windows Server 2022 x64 |
| Deployed version/source | `1.0.0` / `784fa96b9436aee315fae2d7e669a2650dbff794`, tag `v1.0.0` |
| Active directory | `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920` |
| IIS site/application | `LabAuthServer`, root application `LabAuthServer/`, site ID 2 |
| Pool/identity | `LabAuthServerAppPool` / `LAB\svc_labauth` |
| HTTPS binding | `*:443:DC01.lab.local`; URL `https://DC01.lab.local` |
| HTTP binding | `*:80:DC01.lab.local` |
| HTTPS certificate | `Cert:\LocalMachine\My\BD545BA289EBFC645C8C3DC424311975579D7E09` |
| External runtime settings | `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920\appsettings.json`; separately supplied, absent from release ZIP |
| DPAPI protected file | `C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi` |
| JWT signing certificate | `Cert:\LocalMachine\My\94D4AC5345479614B945096CC9CEDE87C48FC51B` |
| SQL | Server `tcp:DC01.lab.local,1433`, database `LabAuthServer`; encrypted connection, certificate trust required |
| Original rollback directory | `C:\Apps\LabAuthServer\Current` (not the active site path) |
| Preserved rollback copy | `C:\Apps\LabAuthServer\Backups\Phase7-20260920\Previous` |
| Evidence/recovery instructions | `C:\Apps\LabAuthServer\Backups\Phase7-20260920` |
| IIS configuration backup | `C:\Windows\System32\inetsrv\backup\Phase7-20260920` |
| Other historical backup locations | `C:\Apps\LabAuthServer\Backups` and `C:\Apps\LabAuthServer\Backup` |

Original ZIP: `C:\Apps\LabAuthServer\Releases\v1.0.0-preparation-20260913-212903\LabAuthServer-1.0.0.zip`.

SHA256: `564f5be016e7f679c32751c4f30488b8482ca57ccec8207c81802a8aee73f6a0`. All 51 deployed package files matched `Artifact-Manifest.json`; the 52-file rollback copy matched `Current`. The remote tag resolves to the source above, and the [existing GitHub release](https://github.com/chriltolakhmer-glitch/LabAuthServer/releases/tag/v1.0.0) advertises the same ZIP digest.

## Health check

Run on the server in PowerShell; also run the HTTPS check from the intended client when investigating client connectivity:

```powershell
curl.exe --silent --show-error --fail --max-time 20 --write-out '\nHTTP %{http_code}\n' https://DC01.lab.local/api/v1/health
if ($LASTEXITCODE -ne 0) { throw 'HTTPS/health check failed' }
Import-Module WebAdministration
Get-WebAppPoolState -Name 'LabAuthServerAppPool'
Get-Website -Name 'LabAuthServer' | Select-Object Name, State, PhysicalPath, ApplicationPool
```

Expected: normal certificate/hostname validation, HTTP 200, `{"status":"Healthy"}`, pool/site Started and the active path above. Never use `-k` or disable certificate checks. Health proves liveness only; it does not prove DPAPI, AD, signing or SQL operation.

## Restart

Use elevated PowerShell when a restart is appropriate. Record the incident/time first. Recycle only this pool; do not use `iisreset` on this domain controller:

```powershell
Import-Module WebAdministration
if ((Get-WebAppPoolState -Name 'LabAuthServerAppPool').Value -eq 'Started') {
    Restart-WebAppPool -Name 'LabAuthServerAppPool'
} elseif ((Get-WebAppPoolState -Name 'LabAuthServerAppPool').Value -eq 'Stopped') {
    Start-WebAppPool -Name 'LabAuthServerAppPool'
} else {
    throw 'Pool is transitioning; inspect its state before retrying'
}
```

Then run health and the authentication checks below. Do not repeatedly recycle a failing pool without checking its diagnostics.

## Authentication smoke check

Use a local interactive client with normal TLS validation. Enter passwords only in its secure prompt; keep JWTs in memory, with no saved requests, transcripts or token screenshots. Give each request a fresh `X-Correlation-ID` GUID and record the status, sanitized role and correlation only.

1. `POST /api/v1/auth/login` with the enabled Reader test user's UPN/password: expect 200 and an issued JWT with role `Reader`.
2. Use that token as Bearer authentication for `GET /api/v1/protected`: expect 200.
3. Log in with an enabled Operator or Administrator test account: expect 200 and its mapped role. Its token must receive 403 at the same Reader-only endpoint.
4. Match SQL `AUTH_LOGIN_SUCCESS` for both logins, `AUTHZ_ACCESS_GRANTED` for Reader access and `AUTHZ_ACCESS_DENIED` for the 403 using the recorded correlations. Denial rows intentionally omit Role; do not treat that as missing audit evidence.

Roles are exact, not hierarchical. Only the highest mapped role is issued; direct membership in an Operator group overrides Reader. Nested membership alone is not expanded by v1.0.0. There is no Operator-specific production endpoint: Operator HTTP 200 on such a resource is **NOT APPLICABLE**. Stay below the shared ten-login-per-minute limit.

The Phase 7 test account was restored to Operator; there is no standing Reader test user in the verified baseline. The existing local helper `C:\Apps\LabAuthServer\Backups\Phase7-20260920\Run-IisSmoke.ps1` supports the tested temporary Reader-to-Operator sequence for the selected account. For a deliberate repeat of that test, review the helper and run:

```powershell
& 'C:\Apps\LabAuthServer\Backups\Phase7-20260920\Run-IisSmoke.ps1' -Phase Post -RuntimePath 'C:\Apps\LabAuthServer\Releases\v1.0.0-20260920'
```

This is an interactive operational test, not a read-only health probe: it temporarily changes only that account's application-role memberships, submits real logins, appends normal SQL audit events and restores membership in cleanup. It prompts locally for each password. Confirm `Status=PASS` and membership-restored evidence in `Post-Smoke.json`; preserve earlier evidence before a rerun because this file is overwritten. Do not use the helper's Pre mode or its fixed v1.0.0 path for a different release/rollback version. Stop on authentication, DPAPI, JWT, SQL or TLS failure; do not weaken controls.

## Logs and audit

- **Application startup:** Windows Event Viewer -> Windows Logs -> Application, provider `IIS AspNetCore Module V2`; inspect nearby `.NET Runtime` errors where present. Do not assume these contain every application log message.
- **Application runtime logging:** the release uses Console/Debug providers. ANCM stdout capture is currently disabled (`stdoutLogEnabled=false`); configured relative path is `.\logs\stdout`, but no active stdout file stream is claimed. `C:\Apps\LabAuthServer\Logs` is currently empty. Do not treat either as a working rolling application log. Enabling temporary diagnostic capture would be a separate runtime configuration change, not part of this handoff.
- **IIS access logs:** W3C logging is enabled for site ID 2 at `C:\inetpub\logs\LogFiles\W3SVC2`; inspect the latest `u_ex*.log` by timestamp, URI and HTTP/substatus. IIS log buffering can delay entries. Correlation headers need not appear in these logs.
- **SQL audit:** `LabAuthServer.Audit.AuditEvents` joined to `Reference.EventTypes`; writes use `Audit.usp_WriteAuditEvent`. Use an existing authorized read identity; do not grant table-read rights to the application merely to inspect evidence.

In an existing SQL query tool, connect to `tcp:DC01.lab.local,1433`, database `LabAuthServer`, with encryption enabled and certificate trust enforced. The following read-only queries show session encryption and recent sanitized events:

```sql
SELECT encrypt_option, net_transport
FROM sys.dm_exec_connections WHERE session_id = @@SPID;

SELECT TOP (30) a.EventTimeUtc, e.EventTypeCode, a.Role,
       a.StatusCode, a.CorrelationId
FROM Audit.AuditEvents AS a
JOIN Reference.EventTypes AS e ON e.EventTypeId = a.EventTypeId
WHERE a.EventTimeUtc >= DATEADD(minute, -30, SYSUTCDATETIME())
ORDER BY a.EventTimeUtc DESC;
```

Match the exact smoke correlations, not just recent row counts. The first query proves the inspector's SQL connection only; application audit writes and the recorded IIS-context test establish application access. Audit is best effort: health/login success alone does not prove persistence. Retention/purge automation is not implemented; no retention period or monitoring service is assumed.

## Backup

Before an operational change, preserve the active application's complete files and matching external configuration/ACLs, artifact ZIP/hash/manifest, IIS site/pool/binding/environment settings, and the protected-secret/certificate references needed on the same machine. Keep backups restricted; never copy plaintext credentials or private keys into source control or release assets. Record UTC time, active path/version and checksums, then verify file parity and required references.

The latest **verified rollback set** is `C:\Apps\LabAuthServer\Backups\Phase7-20260920\Previous`, paired with `Recovery.json`, `Deployment.json`, `Artifact-Manifest.json`, smoke evidence in its parent directory and IIS backup `Phase7-20260920`. This preserves the pre-v1.0.0 deployment; it is not a newly captured backup of the active release. The active v1.0.0 ZIP remains at the path above.

Choose a backup by its recovery record, matching application/configuration, verified hashes and available dependencies—not by directory timestamp alone. The verified previous API DLL hash is `eea12b480062acf586738e29c635673ac5452895a761bddd58d7649697913d53`. No off-host backup or tested bare-machine recovery is claimed. DPAPI is LocalMachine-bound: copying its file to another machine is not a working secret recovery method. Certificates and their private keys remain in the original host's store.

## Rollback

Status: **READY — RESTORE NOT EXECUTED**. For a suspected deployment failure, first retain safe incident evidence and verify `Current` still matches the preserved `Previous` copy, including its `appsettings.json` and dependencies. Confirm its referenced DPAPI file and signing certificate exist. Then the existing path-switch rollback is:

```powershell
Import-Module WebAdministration
$rollbackPath = 'C:\Apps\LabAuthServer\Current'
if (-not (Test-Path -LiteralPath (Join-Path $rollbackPath 'LabAuthServer.Api.dll'))) {
    throw 'Rollback files missing; stop'
}
if ((Get-WebAppPoolState 'LabAuthServerAppPool').Value -eq 'Started') {
    Stop-WebAppPool 'LabAuthServerAppPool'
}
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    if ((Get-WebAppPoolState 'LabAuthServerAppPool').Value -eq 'Stopped') { break }
    Start-Sleep -Seconds 1
}
if ((Get-WebAppPoolState 'LabAuthServerAppPool').Value -ne 'Stopped') {
    throw 'Pool did not stop; do not switch paths'
}
Set-ItemProperty 'IIS:\Sites\LabAuthServer' -Name physicalPath -Value $rollbackPath
Start-WebAppPool 'LabAuthServerAppPool'
```

Repeat HTTPS, health, Reader/non-Reader and correlated SQL checks manually against the restored version; record restored path and previous hash. Preserve the failed release for diagnosis. Do not delete SQL audit history, restore the database, export keys or reset all IIS services.

If `Current` is unavailable, stop and use the preserved `Previous` copy with its matching configuration; its parent is Administrators/SYSTEM-only, so scoped runtime read/traverse access must be arranged before activation. The IIS backup is available for comparing/restoring relevant site/pool settings if they changed; do not blindly restore the entire server configuration on this shared domain controller.

## Configuration changes

Runtime settings, DPAPI secrets, certificate/private-key resources and environment-specific values remain external to the release artifact. The deployed `appsettings.json` is a separately tracked copy beside the DLL, not a ZIP member. The pool's `ActiveDirectory__ServiceAccountUsername` override matched JSON during deployment; inspect override names/precedence without printing secret values when diagnosing drift. Preserve the configuration/ACL backup before changes and repeat the minimum smoke afterward. Do not rebuild, edit the immutable ZIP or change the v1.0.0 tag/release to alter host configuration.

## Small maintenance routine (Phase 9)

Recommended cadence, not a schedule: run the check weekly and after host maintenance; monthly review certificate expiry, installed .NET/IIS versions and vendor support/security notices, service-account memberships and secret/key/file permissions. Before an upgrade, verify backup integrity and the new checksum. No automatic upgrade, restart, ACL repair or scheduled task is introduced.

From the repository root in Windows PowerShell with IIS inspection access:

```powershell
& .\scripts\operations\Test-LabAuthServerMaintenance.ps1 `
    -HealthUrl 'https://DC01.lab.local/api/v1/health' `
    -BackupPath 'C:\Apps\LabAuthServer\Backups\Phase7-20260920\Previous' `
    -ExpectedDeploymentPath 'C:\Apps\LabAuthServer\Releases\v1.0.0-20260920' `
    -ArtifactPath 'C:\Apps\LabAuthServer\Releases\v1.0.0-preparation-20260913-212903\LabAuthServer-1.0.0.zip' `
    -ExpectedSha256 '564f5be016e7f679c32751c4f30488b8482ca57ccec8207c81802a8aee73f6a0' |
    Format-List Check, Status, Detail
$LASTEXITCODE
```

The [script](../../scripts/operations/Test-LabAuthServerMaintenance.ps1) is repeatable and read-only apart from normal HTTP/server logging. It reports the actual IIS path rather than assuming `Current`. PASS/exit 0 means the requested checks passed; WARN/exit 2 means a check was omitted, inventory unavailable or certificate expiry is near; FAIL/exit 1 means a failed/unverifiable required check. Investigate the named check using the recovery checklist. A PASS is not proof of AD login, private-key permission, complete backup recoverability or latest patch installation. Do not run the older external `HealthCheck-LabAuthServer.ps1` for this purpose: it cleans, builds and publishes.

The default HTTPS expiry warning is 30 days (`-CertificateWarningDays`). On WARN, arrange renewal before expiry, verify the new certificate's hostname/chain/private-key access, then deliberately update the relevant binding and run trusted HTTPS plus authentication smoke. Do not disable validation or let the script replace certificates. Review JWT, LDAPS and SQL certificates separately; the checker inventories only the HTTPS binding certificate. Review runtime/IIS updates manually, with backup and post-change smoke; never infer patch currency from a successful runtime inventory.

Runtime file logging is intentionally disabled for this minimal baseline. Existing IIS request logs, correlated SQL audit and Windows startup diagnostics support availability/authentication outcomes; they do not guarantee detailed runtime diagnostics during SQL failures. Reader tests keep the existing temporary-membership-and-restore approach above; no permanent privilege expansion is needed.

### Safe file restore check and off-host copy

The checker reads/hashes every backup file and checks required application/configuration filenames. Before relying on it, also compare a known manifest/parity record and verify matching external configuration, dependencies, DPAPI/certificate references and IIS recovery settings. Backup age alone does not establish validity.

To exercise file restoration, select the verified `Previous` backup, create a **new, empty, restricted directory outside every IIS physical path**, copy the backup there, and compare relative file counts and every SHA256 against the source. Do not overwrite any existing directory, start binaries, change IIS, or treat successful copying as an authentication/DPAPI recovery test. Stop on unreadable files or mismatches.

Phase 9 performed this check at `C:\Apps\LabAuthServer\Backups\Phase7-20260920\Phase9-RestoreCheck-20260921`: 52 files matched. It is a non-production test copy, not the rollback target. Production rollback remains **READY — RESTORE NOT EXECUTED**; off-host recovery is not tested.

Recommend an encrypted copy on existing removable media or another already-controlled host: the verified release ZIP/checksum/manifest; the selected complete application backup including runtime dependencies and matching external settings; `Recovery.json` and sanitized deployment evidence; IIS configuration backup; and a protected recovery inventory of certificate references, ACL requirements and the DPAPI file. Treat settings, IIS backups and protected-secret backups as sensitive; keep them outside Git/release assets. Copy protected secret material only into that restricted encrypted backup. DPAPI files alone cannot recover credentials on another machine, and certificate references do not back up private keys: any off-host recovery must separately provide a secure credential reprovisioning and certificate/key recovery method. No private-key export, off-host copy or new storage infrastructure was performed here.

### Minimal upgrade sequence

1. Obtain the next already-built release and expected SHA256; compare before extracting. Stop on mismatch. Retain the original ZIP.
2. Capture the **actual IIS active path**, matching external settings/ACLs and site/pool/binding configuration. Preserve and hash-check that complete version as the new rollback target; do not assume the historical `Current` directory is latest.
3. Extract into a new versioned directory. Verify artifact files, supply reviewed external configuration separately, and confirm runtime prerequisites. Keep passwords/keys out of the package.
4. Stop only `LabAuthServerAppPool`, switch the existing site's physical path to the new verified directory, and start the same pool/identity. Preserve previous files/configuration; do not modify the old ZIP/tag/release.
5. Run trusted HTTPS, health, anonymous 401, manual Reader 200, non-Reader 403 and correlated SQL audit checks. The existing Phase 7 helper is pinned to v1.0.0: do not use it unchanged for a newer release.
6. On failure, stop the affected pool, restore the captured previous path/settings, start it and repeat checks using the rollback procedure above with that captured path. Record results and retain both versions. No database restore or audit deletion is part of application rollback.
