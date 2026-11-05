# ADR 0004: Own `api/account/register`; Identity's endpoint is answered 404

- Status: Accepted
- Date: 2026-11-05

## Context

`MapIdentityApi<HattoriUser>()` ships a public `POST /account/register` — but a bare Identity account has no roles, and hattori's authorization is permission-based: a self-registered user without the `User` role would pass authentication and then get 403 from every endpoint. Registration here must also create the role on first use and assign it.

## Decision

Keep the identity API for login/refresh/logout/manage, and add `AccountController.Register` (`POST /api/account/register`, anonymous, rate-limited): create the user, idempotently ensure the `User` role with its permission claims (`DefaultRoles.PermissionsFor`), add the user to it, and sign in. A middleware placed after `UseCors` answers Identity's own `POST /account/register` with 404, so there is exactly one registration path.

## Consequences

- One registration flow, and every new account is immediately functional.
- The 404 (not 403/405) makes the closed endpoint look absent rather than broken into.
- Cost: one custom endpoint plus one middleware; if Identity ever grows a "default role on register" hook, both can go.
