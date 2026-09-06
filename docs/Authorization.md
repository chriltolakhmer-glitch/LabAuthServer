# Authorization

Authorization is role-based and fail-closed.

## Policies and roles

The default authorization policy requires an authenticated JWT. Named policies exist for:

- `Reader`
- `Operator`
- `Administrator`

The implemented protected resource uses the Reader policy:

- `GET /api/v1/protected`

Anonymous or invalid-token requests receive `401`. An authenticated principal with an insufficient role receives `403`.

## Group mapping

The `Authorization:GroupToRoleMappings` configuration is an explicit allowlist. The current approved mapping model is:

| AD group identifier | Role |
| --- | --- |
| `GG-APP-ADMIN` | `Administrator` |
| `GG-APP-APPROVER` | `Operator` |
| `GG-APP-USER` | `Reader` |
| `GG-APP-REPORT` | `Reader` |

Matching is case-insensitive, trims identifiers, ignores unknown groups, and rejects malformed configuration. Role precedence is `Administrator > Operator > Reader`. The login flow places only the highest-precedence role in the token.

The configured maximum group count is enforced. Missing approved membership produces no role and cannot grant authorization. No nested-group expansion or request-time AD revalidation is implemented.

## Public endpoints

The current public endpoint allowlist contains only the health and login routes. Other controller endpoints are protected by the default policy or an explicit named policy.
