# 08 · Maintainers' guide

Recipes for the changes that actually happen, in the order the layers demand. Every recipe follows the same spine: contract in `.Core`, implementation in `.Persistence`, surface in `.Web`, proof in `.Web.Tests`.

## Add an endpoint

Example: a `PATCH` that only toggles completion.

1. **DTO** (`Hattori.Core/DTOs/`): a request record with data annotations, a response record or reuse an existing one.
2. **Interface** (`Hattori.Core/Interfaces/`): one method on the repository, taking the owner ID first, a `CancellationToken` last, returning DTOs only.
3. **Implementation** (`Hattori.Persistence/Repositories/`): EF query scoped by owner; reads project + `AsNoTracking()`; writes save their own change; never `Remove()` on a soft-deletable entity.
4. **Controller action** (`Hattori.Web/Controllers/`): `[Http…]`, `[Authorize(Policy = Permissions.…)]`, `[EndpointDescription("…")]`, `CancellationToken` parameter, status helpers for expected failures (`NotFound`, `Conflict`, …). No `DbContext`, ever.
5. **Example** in `Hattori.Web/Hattori.Web.http`.
6. **Tests**: a repository test against the 5433 database + (for a new route) an assertion in the pipeline test.
7. `dotnet build` must stay warning-free; `dotnet test` green.

## Add a migration

```shell
cd Hattori.Web
dotnet ef migrations add <Name> --project ../Hattori.Persistence
dotnet ef database update --project ../Hattori.Persistence   # dev database
```

Read the generated migration before committing — `Up()` and `Down()` both. Change the model only in `HattoriDbContext.OnModelCreating`; never hand-edit an applied migration.

## Add a setting

1. Constant name `HATTORI_<NAME>`; decide the debug default.
2. A `Lazy`-caching static class in `Hattori.Core/Configurations/` calling `EnvironmentVariables.Required(name, debugDefault)` — this is the only place `Environment.GetEnvironmentVariable` may appear.
3. Touch it in `Program.cs`'s fail-fast block if the app cannot run without it.
4. Dev value in `Hattori.Web/Properties/launchSettings.json`; row in the README's environment-variable table.
5. Fixed choices? Parse into an enum so a typo fails loudly.

## Add a permission

1. Constant in `Permissions` (+ nested class if new area) and into `Permissions.ALL` — the policy then exists automatically.
2. Grant it: add to `DefaultRoles.PermissionsFor` for the role that gets it.
3. Use it: `[Authorize(Policy = Permissions.<Area>.<VERB>)]` on the actions.
4. Existing users pick it up at next sign-in (claims are baked into the cookie — [ADR 0006](decisions/0006-permissions-in-cookie.md)); re-seed roles with `Hattori.Tools` → `dotnet run -- roles seed` after changing claims on existing roles.

## Add a test

- Repository/service behavior → build the context with `HattoriDatabaseRegistration.BuildOptions(DatabaseConnectionTarget.Test)`, migrate in `[OneTimeSetUp]`, clean your tables in `[TearDown]` (`IgnoreQueryFilters().ExecuteDeleteAsync()`), construct the class directly.
- Whole pipeline (routing, auth, policies) → the `WebApplicationFactory<Program>` pattern in `WebPipelineTests`; `TestEnvironment` has already pointed the settings at the test database.
- External services → hand-written fakes implementing the `.Core` interface; no mocking library.
- One fixture: `dotnet test --filter "FullyQualifiedName~TodoListRepositoryTests"`.

## Where code goes

| I'm writing… | It goes in | It never goes in |
| --- | --- | --- |
| An entity, DTO, interface, permission, setting, logger piece | `Hattori.Core` | — |
| EF query, migration, interceptor, claims factory | `Hattori.Persistence` | Controllers |
| Controller, authorization handler, middleware order | `Hattori.Web` | `DbContext` access, EF packages |
| Admin/seeding job | `Hattori.Tools` | A web endpoint |
| A test | `Hattori.Web.Tests` | The EF in-memory provider |

## Review checklist (from the dotnet skill)

Style (the compiler catches most of it): private camelCase no `_`; private constants `all_lower`, public `ALL_UPPER`; no `var` for built-in types; explicit `new` (never target-typed `new()`, collection expressions, primary constructors); braces on their own line; expression bodies for single-expression members; XML doc on every class and member; usings written out, ordered `System.*` first; license header present; exceptions logged with `logger.LogError(ex, "…")`.

Rules that must not break (AGENTS.md carries the short list): repositories between controllers and `DbContext`; one-way references; `[Authorize]` + policy everywhere except deliberate publics; soft delete, audit by interceptor; UTC moments, `DateOnly` dates, no `DateTime.Now`; env-var settings only; real-Postgres tests.

Call out style findings as style, separate from real bugs, so the important notes don't drown.

Back to the [index](README.md).
