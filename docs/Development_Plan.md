# Development Plan

## Governance rule

Each feature begins with architect-approved requirements and acceptance criteria. Before implementation, review the project status, architecture, coding standard, and this plan. Update architecture and API documentation whenever an approved design changes.

## Completed foundation

- Four-layer solution structure and project references.
- .NET 10 SDK pin (`10.0.400`).
- Base ASP.NET Core configuration, logging, HTTPS redirection, and ProblemDetails handling.
- Versioned health endpoint: `GET /api/v1/health`.
- Health endpoint integration test.
- WeatherForecast and default template code removed.

## Planned work

1. Define and approve configuration requirements and acceptance criteria.
2. Define and approve the LDAPS connection-test design for `DC01.lab.local:636`.
3. Implement the approved Active Directory integration in Infrastructure with Application abstractions.
4. Define and approve authentication, token, authorization, database, and secrets designs before implementation.
5. Implement each approved vertical slice with validation, error handling, API documentation, and relevant tests.

## Exit criteria for each major task

- The change follows the approved architecture and coding standard.
- No secrets are committed or logged.
- Relevant unit and integration tests pass.
- `dotnet build` completes with zero warnings and zero errors.
- `docs/Project_Status.md` records the completed work, verification results, and outstanding decisions.
