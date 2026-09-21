# Phase 9 — Maintenance & Reliability

2026-09-21: **COMPLETE** for the small maintenance baseline below. Phase 7 deployment and Phase 8 handoff remain complete. No new service, dependency, schedule or application change is required. Use the existing [operations runbook](../../operations/LabAuthServer-Operations-Runbook.md) and [recovery checklist](../../operations/LabAuthServer-Recovery-Checklist.md).

| Area | Solo-developer action |
| --- | --- |
| Health | Run the [maintenance script](../../../scripts/operations/Test-LabAuthServerMaintenance.ps1) manually each week and after host/configuration maintenance: inspect IIS state and actual physical path, identify application version, request trusted HTTPS health. Health is liveness, not authentication acceptance. |
| Certificate/dependency maintenance | Review expiry monthly; the script checks the HTTPS binding certificate with a configurable 30-day warning. Manually review JWT/LDAPS/SQL certificates and installed .NET/IIS versions against vendor support/security notices. Inventory is not proof of patch currency. No automatic upgrade or certificate replacement. |
| Backup/recovery | Verify the recorded backup's files are readable; compare hashes before use. A file-only restore test into a new restricted, non-production directory is safe without executing binaries or changing IIS. Keep production rollback **READY — RESTORE NOT EXECUTED**. Recommend an encrypted offline/off-host copy as described in the runbook; no platform added. |
| Upgrade | Verify the separately released new artifact checksum; back up the actual active path and matching external settings; stage a new versioned directory; switch the existing IIS pool/site during the intended window; run manual Reader/non-Reader and SQL smoke; retain the previous path/configuration for rollback. Never overwrite v1.0.0 assets. |
| Security hygiene | Monthly and after relevant changes, review service-account groups, directory/key/secret ACLs and certificate private-key access. Preserve least privilege; do not print secrets or automatically repair permissions. Repeat IIS-context authentication smoke when access changes. |

## Implementation and evidence

The old external `C:\Apps\LabAuthServer\Scripts\HealthCheck-LabAuthServer.ps1` performs cleanup/build/publish and assumes `Current` is active. It was inspected but not executed or changed. The new parameterized checker fills the missing read-only maintenance role; existing deployment and interactive smoke tools remain separate.

- Live check: **8 PASS, 0 WARN, 0 FAIL**, exit 0. Actual IIS path is `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920`, product version 1.0.0; `Current` remains the previous deployment. HTTPS health 200; HTTPS certificate expires `2027-08-26T14:50:33Z`; artifact SHA256 matches `564f5be016e7f679c32751c4f30488b8482ca57ccec8207c81802a8aee73f6a0`.
- Warning exercise: omitted artifact verification and a 365-day certificate warning threshold produced WARN, exit 2. Negative exercise: wrong expected path/hash, missing backup and HTTP URL produced FAIL, exit 1. No production value was changed to induce these results.
- File recovery: copied `C:\Apps\LabAuthServer\Backups\Phase7-20260920\Previous` into the new restricted `C:\Apps\LabAuthServer\Backups\Phase7-20260920\Phase9-RestoreCheck-20260921`; **52/52 file hashes matched**. Copy retained for inspection, never hosted or executed. This proves file restoration only, not full service or off-host recovery.
- Existing IIS access logs and all four recorded Phase 7 post-deployment SQL audit rows were readable. Together with Windows ANCM startup diagnostics they are adequate for this minimal request/outcome baseline; runtime file logging remains intentionally disabled. Detailed internal diagnostics during an outage are not guaranteed.
- Reader testing retains the documented temporary direct-membership sequence with restoration; no accounts/memberships were changed for Phase 9.
- Validation: PowerShell syntax, manual positive/warning/negative execution, local Markdown links/fences, whitespace and Git diff checks. No build or deployment test suite was required or run; no application source changed.

Remaining limitations: health does not test dependencies; backup plausibility is not a complete dependency/restore proof; production rollback and off-host recovery remain untested; no durable runtime file log, automated monitoring or retention/purge is introduced. No tag, GitHub release, ZIP, deployed files, production configuration, certificates, AD or SQL schema changed.
