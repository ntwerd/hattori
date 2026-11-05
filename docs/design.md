# Design notes

How the hattori todo API is put together, and why. The conventions come from the dotnet skill at `.agents/skills/helloyeew-dotnet-skill/`.

## Data model

- `TodoList` — a user's named, optionally colored group of items. Names are unique per user among live lists, stored lowercase (the skill's tag pattern), enforced by a partial unique index `ON (OwnerId, Name) WHERE "IsDeleted" = false`, so a soft-deleted list frees its name.
- `TodoItem` — belongs to a `TodoList` (cascade delete). `DueDate` is a `DateOnly` (`date` column): a date with no time and no zone is never converted, per the skill's time rules. `CompletedAt` is a UTC moment (`timestamp with time zone`), stamped once on first completion and cleared on reopening.
- `OwnerId` has no foreign key to the users table on purpose: ownership is enforced in every repository query, which keeps repository tests free of identity rows.
- Soft delete everywhere (`ISoftDeletable`) with EF global query filters; audit fields (`IAuditable`) are stamped by `AuditInterceptor` from the current user — never set by hand.

## Permissions

Roles carry permissions as `permission` claims; the claims factory copies the user's roles' permission claims into the cookie at sign-in; `PermissionAuthorizationHandler` matches `[Authorize(Policy = Permissions…)]` against those claims. One policy per constant in `Permissions.ALL`, registered in a loop in `Program.cs`.

Self-registered accounts get the `User` role (the register endpoint creates it idempotently). The identity API's own `/account/register` is answered with 404 by a middleware, because registration here must also assign the default role; login, refresh and logout stay on the identity API endpoints under `/account`, rate-limited per client address.

## Configuration

No settings in `appsettings.json`; every app setting is a `HATTORI_` environment variable read once in `Hattori.Core/Configurations/`, cached, with debug-default fallback (logged warning) or startup failure in Release.

## Testing

Repository tests build the context directly (`HattoriDatabaseRegistration.BuildOptions(DatabaseConnectionTarget.Test)`), migrate once per fixture, and clean their tables after each test. The pipeline test boots the whole app with `WebApplicationFactory<Program>` and walks register → create list → create item → complete → soft-delete, plus the 401-no-redirect and health checks.
