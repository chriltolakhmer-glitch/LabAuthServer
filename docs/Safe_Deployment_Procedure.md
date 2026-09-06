# Safe LabAuthServer Deployment Procedure

Use `C:\Apps\LabAuthServer\Scripts\Deploy-LabAuthServerSafe.ps1` for future application deployments. The script is not a release builder and must receive an immutable, already-approved release directory.

## Safety model

1. Copy the complete release runtime, excluding only `release-manifest.txt`, into a unique directory under `C:\Apps\LabAuthServer\Staging`.
2. Copy files individually with `-LiteralPath` and `-ErrorAction Stop`; every copy error is collected and reported. A failed staging copy leaves `Current` untouched.
3. Validate staging before production replacement:
   - exactly 52 runtime files;
   - no missing, unexpected, or SHA-256-mismatched files;
   - `UserSearchBaseDn` is `DC=lab,DC=local`;
   - Audit connection string is present and timeout is 5 seconds;
   - no development settings, PDBs, or private-key artifacts.
4. Create a timestamped complete backup of `Current` under `C:\Apps\LabAuthServer\Releases`.
5. Stop only `LabAuthServerAppPool` and wait for `Stopped`.
6. Replace `Current` from the validated staging directory.
7. Validate `Current` against staging again. If replacement or validation fails, restore the complete rollback backup, validate rollback parity, and leave the pool stopped.
8. Start only `LabAuthServerAppPool` after validation passes. The IIS site and bindings are not changed.
9. Verify the site and pool state, HTTPS health `200`, and anonymous protected endpoint `401`.
10. Remove the temporary staging directory only after successful completion.

## Invocation

Run from an elevated PowerShell session:

```powershell
& 'C:\Apps\LabAuthServer\Scripts\Deploy-LabAuthServerSafe.ps1' `
    -ReleasePath 'C:\Apps\LabAuthServer\Releases\<approved-release>'
```

Preview staging and all pre-replacement validation without touching production:

```powershell
& 'C:\Apps\LabAuthServer\Scripts\Deploy-LabAuthServerSafe.ps1' `
    -ReleasePath 'C:\Apps\LabAuthServer\Releases\<approved-release>' `
    -WhatIf
```

The output reports the source release, staging path, expected/copied counts, copy errors, missing/unexpected/mismatched files, backup path, rollback status, IIS state, and smoke-test statuses. Do not use a piped `Copy-Item` deployment command and do not clear `Current` before staging validation passes.