# Operations and Handover

## Operational checklist

### Pre-deployment

- confirm `.NET 10` is installed
- confirm IIS site and app pool exist
- confirm `Current` directory is present
- confirm `Build` and `Releases` directories are writable
- confirm the DPAPI secret file exists and the runtime identity can read it
- confirm the certificate is installed in the configured store
- confirm SQL server connectivity and permission mapping
- verify the release package is staged and hash-checked

### Deployment

- run `C:\Apps\LabAuthServer\Scripts\Deploy-LabAuthServerSafe.ps1`
- permit the script to stage the release and validate parity
- ensure app pool stop/start occurs only during the script workflow
- confirm replacement copy matches the release package exactly

### Post-deployment

- confirm the site is running
- confirm the app pool is running
- request the health endpoint over HTTPS
- confirm anonymous access to the protected endpoint returns `401`
- confirm login requests still require HTTPS
- confirm SQL audit events are recorded when requests are made

### Rollback

- use the release backup stored under `Releases`
- restore the prior `Current` directory state with the script rollback logic when validation fails
- keep the backup until release verification is complete

### Authentication test

- POST a valid AD login to `/api/v1/auth/login` over HTTPS
- confirm a JWT is issued
- confirm the token contains the expected claims and `role`
- confirm bearer authorization works against `/api/v1/protected`

### Authorization test

- anonymous protected call -> `401`
- invalid token -> `401`
- authenticated non-reader role -> `403` if not allowed by policy
- Reader role -> `200`

### Health test

- `GET /api/v1/health` -> `200`
- HTTP redirect to HTTPS should occur for non-HTTPS health requests

## Release process

The release process follows these steps:

1. restore and build the solution in Release mode
2. run the unit and integration test suites
3. publish the API to a clean release directory
4. verify the runtime package contents and hash parity
5. create a versioned package under `Releases`
6. run the safe deployment script against the target server
7. validate health and authorization smoke tests
8. keep the backup until the target release is confirmed stable

## Final acceptance criteria

The Phase 17 final acceptance criteria are satisfied by the validated production behavior observed in the project history:

- HTTP 200 on fresh login
- issued JWT successful
- protected endpoint with valid token returns `200`
- anonymous protected request returns `401`
- malformed bearer token returns `401`
- full suite passes at 173 tests with 0 failures
- dependency vulnerability scan shows no advisories
- IIS site and app pool remain started
- safe deployment and current runtime parity checks pass

The repository documentation must reflect only this verified result and must not claim features beyond the implemented boundary.
