# Coding Standard and SOP

This Markdown document is the working project instruction. The original source is retained at `docs/SOP/Auth_Server_Coding_Standard_and_SOP.docx`.

## Technology and naming

- Use C#, ASP.NET Core, and .NET 10.
- Use PascalCase for types and public members; use camelCase for parameters and local variables; use `_camelCase` for private fields.
- Keep one main type per file where practical and match the file name to the primary type.
- Use meaningful responsibility suffixes, such as `Controller`, `Service`, `Repository`, `Validator`, `Request`, `Response`, `Options`, and `Exception`.
- Avoid vague names such as `Helper`, `Common`, `Misc`, and numbered placeholder names.

## Layer responsibilities

- Domain stays independent of application, infrastructure, and HTTP concerns.
- Application contains use cases, interfaces, DTOs, validation, and orchestration; it does not depend on HTTP details.
- Infrastructure implements databases, Active Directory, token persistence, and other technical integrations.
- API is the HTTP boundary. Controllers stay thin and compose Application and Infrastructure through dependency injection.

## C# and API practices

- Keep nullable reference types enabled and warnings at zero.
- Prefer `async`/`await` for I/O and suffix asynchronous methods with `Async`.
- Use dependency injection; do not manually construct services inside controllers.
- Use DTOs for external input/output and validate all external input.
- Version public API routes, use lower-case resource-oriented routes, and return appropriate HTTP status codes.
- Use consistent ProblemDetails-style error responses and centralized exception-to-HTTP handling.
- Use `ILogger<T>` with structured logging. Do not log passwords, tokens, raw authorization headers, or sensitive directory responses.

## Security and configuration

- Use HTTPS outside local development.
- Never store plaintext passwords, access tokens, refresh tokens, keys, certificates, or secrets in source control or logs.
- Use non-secret defaults in `appsettings.json`; use environment variables, user secrets, or an approved secret store for sensitive values.
- Use LDAPS with production certificate validation for Active Directory authentication.
- Do not disable TLS certificate validation as a permanent solution.

## Data, testing, and delivery

- Use plural table names consistently if database storage is introduced. Store timestamps in UTC and keep migrations under source control.
- Unit tests cover business logic without real infrastructure; integration tests cover API and infrastructure boundaries.
- Run `dotnet restore`, `dotnet build`, and `dotnet test` for applicable implementation work.
- Do not commit secrets, `bin`, `obj`, user-specific files, or local databases.
- Update `docs/Project_Status.md` after major implementation work.

## Approval boundaries

Do not introduce packages, change layer dependencies, change authentication design, change database design, or alter project requirements without project-architect approval. Report conflicts with this standard instead of guessing.
