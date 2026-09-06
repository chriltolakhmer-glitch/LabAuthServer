# Deployment and Operations

## IIS deployment

The live production deployment is configured as follows:

- IIS site: `LabAuthServer`
- application pool: `LabAuthServerAppPool`
- app pool identity: `ApplicationPoolIdentity`
- physical path: `C:\Apps\LabAuthServer\Current`
- HTTPS binding: configured to the approved certificate for the site
- ASP.NET Core hosting: .NET 10 ASP.NET Core hosting bundle required on the Windows server

The deployment keeps the live application under `Current` and uses staging and backup verification before any production replacement occurs.

## Deployment package structure

The release package contains the runtime files required for the application and excludes source, tests, developer configuration, private keys, DPAPI files, and PDBs. Validation is performed against a fixed expected runtime-file count and a hash parity check.

The safe deployment script uses the following workflow:

1. stage a validated release package in `C:\Apps\LabAuthServer\Staging`
2. validate file parity and configuration safety
3. create a rollback backup of the existing `Current` directory
4. stop `LabAuthServerAppPool`
5. replace `Current` with the staged runtime files
6. validate the replacement against the source package
7. start the app pool
8. run health and authorization smoke checks
9. leave the backup in `Releases` for rollback if needed

## Safe deployment script

The implemented procedure is defined in:

- `C:\Apps\LabAuthServer\Scripts\Deploy-LabAuthServerSafe.ps1`

This script is designed to prevent the partial-copy problem by:

- copying the release package into a staging directory first
- checking expected runtime count and expected file hashes before production replacement
- ensuring no development configuration or secret-bearing artifacts are present
- stopping the app pool before replacing files
- automatically restoring the previous `Current` directory if replacement validation fails
- verifying the health endpoint and protected endpoint response codes after deployment

## Rollback approach

Rollback is performed by restoring the last valid `Current` backup from the `Releases` folder. The script explicitly verifies the backup before replacing the live directory and will restore the previous copy if the deployment validation fails.

## Post-deployment validation

The script checks:

- IIS site state is `Started`
- app pool state is `Started`
- health endpoint returns `200`
- anonymous access to `/api/v1/protected` returns `401`

This confirms the site is serving the application and that the default authorization protection is active.
