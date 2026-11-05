# ADR 0006: Permissions baked into the cookie as role claims

- Status: Accepted
- Date: 2026-11-05

## Context

Every endpoint needs `[Authorize(Policy = …)]` checks against permissions. The permissions a user has come from their roles, so each request could resolve roles → permissions from the database — a query (or cache) on every authorization decision — or the permissions could travel with the principal.

## Decision

Roles carry permissions as `permission` claims (added at role creation by the register endpoint and the tools). `HattoriUserClaimsPrincipalFactory` copies the user's roles' permission claims into the principal whenever a cookie is issued; `PermissionAuthorizationHandler` then checks claims per request with zero database access. One policy per constant in `Permissions.ALL` is registered in a loop.

## Consequences

- Authorization is self-contained per request: fast, no cache to invalidate, works for the cookie wherever the app runs.
- Cost: **freshness** — a role's permissions apply to a signed-in user only at their next sign-in (or token refresh), not instantly. Acceptable for this app; a permission-revocation requirement would add a security-stamp revalidation or a per-request lookup.
- Testing is easy: the pipeline test just registers and acts, exactly like a client.
