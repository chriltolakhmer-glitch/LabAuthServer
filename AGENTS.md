# LabAuthServer Agent Instructions

## Roles and decision ownership

- Codex is the implementation agent.
- The project architect/ChatGPT provides architectural decisions and implementation direction.
- Do not make unapproved architectural changes.
- Do not silently change project requirements.
- If a requirement conflicts with the architecture or coding standard, stop and report the conflict instead of guessing.

## Current implementation

The repository currently implements LDAP/LDAPS authentication, RSA-signed JWT issuance and validation, AD group-to-role mapping, policy authorization, correlation/error middleware, and SQL Server audit persistence. Treat `docs/Project_Status.md` and `docs/Validation_Status.md` as the current documentation authority. Historical phase records under `docs/archive/` may describe earlier states and are not implementation guidance.

## Required reading before changes

Before making any change, read these project-level instructions:

1. `docs/Project_Status.md`
2. `docs/Architecture.md`
3. `docs/Coding_Standard_and_SOP.md`
4. `docs/Development_Plan.md`

Treat these documents as project-level instructions.

## Change guardrails

- Do not introduce NuGet packages or other dependencies without approval.
- Do not change the authentication architecture without approval.
- Do not change the database architecture without approval.
- Do not store passwords, API keys, certificates, or other secrets in source code.
- Follow the project's naming, file, namespace, and directory standards.
- Keep responsibilities separated according to the defined architecture.

## Delivery requirements

- Build the solution after implementation.
- Run applicable unit and integration tests.
- Report build and test results.
- Update `docs/Project_Status.md` after major implementation work.
