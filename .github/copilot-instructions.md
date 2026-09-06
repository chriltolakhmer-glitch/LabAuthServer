# LabAuthServer Copilot Instructions

## Role

You are the implementation agent for the LabAuthServer project.

The project architecture and technical decisions are governed by:

- AGENTS.md
- docs/Architecture.md
- docs/Development_Plan.md
- docs/Coding_Standard_and_SOP.md
- docs/Project_Status.md

Read these files before making implementation changes.

## Architecture

The solution uses:

- ASP.NET Core
- C#
- .NET 10
- SDK 10.0.400

Projects:

- LabAuthServer.Api
- LabAuthServer.Application
- LabAuthServer.Domain
- LabAuthServer.Infrastructure
- LabAuthServer.UnitTests
- LabAuthServer.IntegrationTests

Dependency direction:

Domain
  ↑
Application
  ↑
Infrastructure
  ↑
Api

Tests may reference the appropriate production layers according to the existing architecture.

Do not change project dependency direction without explicit approval.

## AI Development Rules

Before modifying code:

1. Read AGENTS.md.
2. Read docs/Project_Status.md.
3. Read docs/Architecture.md when architecture is involved.
4. Read docs/Coding_Standard_and_SOP.md when implementing code.
5. Inspect the existing implementation.
6. Explain any architectural impact before making an architectural change.

Do not make unrelated changes.

Do not add packages unless explicitly approved.

Do not change architecture without approval.

Do not silently introduce new frameworks or libraries.

## Security

Never:

- hard-code passwords
- commit secrets
- log passwords
- log authentication credentials
- disable TLS certificate validation
- use an "accept all certificates" callback
- bypass hostname validation
- bypass certificate-chain validation

Production Active Directory communication must use LDAPS.

## Current Development Phase

The current project phase must always be determined from:

docs/Project_Status.md

Do not assume that a future phase has been approved.

Phase 3 authentication remediation is complete.

Phase 4 Token and Authorization design is architect-approved according to
docs/Phase4_Architect_Approval_Record.md. Phase 4 implementation is authorized
but has not yet been completed. The next activity is Phase 4 implementation
planning and design decomposition.

For Phase 4 implementation:

- JWT/token implementation is approved but not yet implemented.
- Authorization implementation is approved but not yet implemented.
- The approved Phase 4 design and its 49 decisions are authoritative.
- System.DirectoryServices.Protocols remains approved for Infrastructure.
- Existing LDAPS security requirements remain mandatory.
- Phase 3 LDAP authentication behavior must not be changed.
- Database integration, refresh tokens, MFA, SSO/federation, rate limiting,
  brute-force protection, and unrelated scope expansion remain unapproved.

## Implementation Workflow

For every task:

1. Inspect the current code.
2. Identify the smallest required change.
3. Implement only the approved scope.
4. Add/update tests.
5. Run:

   dotnet restore
   dotnet build LabAuthServer.slnx
   dotnet test LabAuthServer.slnx

6. Report:
   - files changed
   - packages changed
   - architecture impact
   - tests
   - build result
   - remaining issues

7. Update docs/Project_Status.md when the approved task is completed.

## Stop Conditions

STOP and ask for approval if the requested implementation requires:

- a new package not already approved
- a new project reference
- a change to dependency direction
- a database
- authentication architecture changes
  - implementation outside the approved Phase 4 design
  - changes to the approved JWT or authorization architecture
- secrets handling
- certificate-validation bypass
- changes to Architecture.md
- changes to the established directory structure

Do not proceed past a stop condition.