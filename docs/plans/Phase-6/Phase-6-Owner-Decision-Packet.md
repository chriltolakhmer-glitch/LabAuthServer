# Phase 6 Owner and Architecture Decision Packet

## 1. Status summary

P6-D1 THROUGH P6-D6 AND P6-D14 OWNER APPROVED — P6-D7 THROUGH P6-D13 INTENTIONALLY DEFERRED PENDING OPERATIONS / DBA / ENVIRONMENT INPUT — P6-I1 APPROVED IMPLEMENTATION DIRECTION — NO PHASE 6.2 THROUGH 6.7 IMPLEMENTATION AUTHORIZED

This packet records the owner-approved Phase 6 governance decisions. It does not authorize implementation of Phase 6.2 through 6.7, release, deployment, customer delivery, signing-key operations, tag creation, release publication, or production license operation.

## 2. Verified baseline

Current planning commit:

- SHA: `6c88d8e74c16c6ae63445c8c6be8669f34e9c8c2`
- message: `Plan remaining Phase 6 work`
- author/committer: `ALOT <chriltola.khmer@gmail.com>`
- GitHub commit verification: `verified: true`, `reason: valid`
- exact hosted `LabAuthServer CI` run: #41, ID `34702672146`
- run status: `completed`
- run conclusion: `success`
- head SHA: `6c88d8e74c16c6ae63445c8c6be8669f34e9c8c2`

## 3. Owner-approved decisions

### P6-D1 — Authorization fallback

Owner-approved wording:

`AUTHENTICATED BY DEFAULT; EXPLICIT PUBLIC ENDPOINTS MUST USE ALLOWANONYMOUS; ROLE-PROTECTED ENDPOINTS KEEP THEIR EXPLICIT POLICIES; FUTURE UNANNOTATED APPLICATION ENDPOINTS REQUIRE AUTHENTICATION; UNMATCHED ROUTES RETAIN NORMAL 404 ROUTING BEHAVIOR; UNAUTHENTICATED REQUESTS RECEIVE 401 AND AUTHENTICATED REQUESTS WITHOUT REQUIRED AUTHORIZATION RECEIVE 403`

Status: OWNER APPROVED

### P6-D2 — Success-path audit identity handling

Owner-approved wording:

`PRESERVE AUTHORIZED ACCESS; DO NOT TRUNCATE IDENTITY; OMIT USERNAME/SUBJECT FROM THE AUDIT EVENT WHEN THEY EXCEED THE APPROVED AUDIT FIELD LIMIT; RECORD A SAFE IDENTITY-OMITTED INDICATOR; PRESERVE THE REMAINDER OF THE VALID AUDIT EVENT`

Status: OWNER APPROVED

### P6-D3 — Licensing initialization failure

Owner-approved wording:

`FAIL SAFE TO A USABLE RESTRICTED LICENSE POLICY; GRANT NO COMMERCIAL FEATURES; RECORD A SAFE OPERATOR DIAGNOSTIC; DO NOT LEAVE NORMAL POLICY RESOLUTION IN A BROKEN/THROWING STATE`

Status: OWNER APPROVED

### P6-D4 — License expiry while process remains running

Owner-approved wording:

`PRESERVE STARTUP-CACHED LICENSE POLICY UNTIL APPLICATION RESTART OR AN EXPLICITLY APPROVED RELOAD MECHANISM; DOCUMENT THAT EXPIRY IS EVALUATED AT POLICY LOAD; CONTINUOUS RUNTIME EXPIRY ENFORCEMENT REQUIRES A SEPARATE FUTURE CONTRACT/IMPLEMENTATION DECISION`

Status: OWNER APPROVED

### P6-D5 — Unknown feature/limit identifiers

Owner-approved wording:

`PRESERVE VERSION 1 COMPATIBILITY: STRUCTURALLY VALID UNKNOWN IDENTIFIERS MAY BE PARSED/VALIDATED, BUT RUNTIME POLICY ACCESS MUST DENY UNKNOWN FEATURES/LIMITS; THE APPROVED ISSUER MUST NOT NORMALLY ISSUE UNSUPPORTED IDENTIFIERS; STRICT VALIDATOR REJECTION REQUIRES A SEPARATE VERSIONED CONTRACT DECISION`

Status: OWNER APPROVED

### P6-D6 — Commercial enforcement claims

Owner-approved wording:

`ONLY CAPABILITIES WITH DEMONSTRATED RUNTIME ENFORCEMENT MAY BE REPRESENTED AS COMMERCIALLY ENFORCED PRODUCT FEATURES; A FEATURE/LIMIT CATALOG ENTRY OR POLICY API ALONE DOES NOT MEAN PRODUCT FUNCTIONALITY EXISTS OR IS ENFORCED`

Status: OWNER APPROVED

### P6-D14 — Release-tag signing policy

Owner-approved wording:

`SIGNED RELEASE TAGS FOR FUTURE OFFICIAL RELEASES, USING AN APPROVED SIGNING PROCEDURE AND VERIFICATION PROCESS; NO TAG OR SIGNING OPERATION IS AUTHORIZED BY THIS POLICY DECISION ALONE`

Status: OWNER APPROVED — POLICY

## 4. Deferred operational decisions

These are intentionally deferred and must remain open gates for later Phase 6 work, not unresolved omissions:

- P6-D7: DEFER PENDING OPERATIONS/DBA INPUT
- P6-D8: DEFER PENDING OPERATIONS/DBA INPUT
- P6-D9: DEFER PENDING OPERATIONS/DBA INPUT
- P6-D10: DEFER PENDING OPERATIONS/DBA INPUT
- P6-D11: DEFER PENDING OWNER DECISION AND OPERATIONAL CONTRACT
- P6-D12: DEFER PENDING TARGET-ENVIRONMENT EVIDENCE
- P6-D13: DEFER PENDING OPERATIONS/DBA/OWNER INPUT

## 5. Approved implementation direction

### P6-I1 — LicenseIssuer release qualification

Owner-approved direction:

`P6-I1 LICENSEISSUER RELEASE QUALIFICATION: INCLUDE LabAuthServer.LicenseIssuer EXPLICITLY IN RELEASE QUALIFICATION / SOLUTION BUILD COVERAGE`

Classification: NO OWNER DECISION REQUIRED — APPROVED IMPLEMENTATION DIRECTION

This does not implement the change now. It belongs to Phase 6.4 implementation review.

## 6. Deferred provider selections

Keep explicitly deferred until first-release preparation:

- exact customer delivery provider/channel;
- exact manifest-storage provider/location;
- exact release-register product/location.

These remain first-release prerequisites and require separate approval before use.

## 7. Professional dependencies

Separate from owner approval and still required:

- proprietary/evaluation legal review;
- commercial terms review;
- copyright/legal wording review;
- retention/support contractual review.

Owner approval does not replace these reviews.

## 8. Non-authorizations

This packet does not authorize:

- Phase 6.2 through 6.7 implementation;
- release creation;
- deployment;
- key generation or signing operation;
- tag creation;
- artifact publication;
- production license issuance;
- repository-setting changes;
- Phase 5.6 changes.

## 9. Final decision status

`P6-D1 THROUGH P6-D6 AND P6-D14 OWNER APPROVED — P6-D7 THROUGH P6-D13 INTENTIONALLY DEFERRED PENDING OPERATIONS / DBA / ENVIRONMENT INPUT — P6-I1 APPROVED IMPLEMENTATION DIRECTION — NO PHASE 6.2 THROUGH 6.7 IMPLEMENTATION AUTHORIZED`
