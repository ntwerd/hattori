# AGENTS.md

Todo application API service (`hattori`) on ASP.NET Core, and the PoC of `helloyeew-dotnet-skill`, which governs every line of C# here.

## Use the dotnet skill

All C# / .NET work here (writing, reviewing, refactoring, scaffolding a solution
or csproj) follows **`helloyeew-dotnet-skill`** — load that skill, or read
`.agents/skills/helloyeew-dotnet-skill/SKILL.md`, before touching any C#.

Its style intentionally deviates from standard .NET conventions; default habits
will be wrong. Highlights (the skill has the full rules):

- Private members camelCase with no `_` prefix; private constants `all_lower`,
  public `ALL_UPPER`.
- No `var` for built-in types (`int count = 0;`); explicit `new`, never
  target-typed `new()` or collection expressions.
- XML doc comment on every class and member; `ImplicitUsings` disabled, so every
  file lists its usings.
- Solutions are `.slnx`; tests use NUnit (not xUnit); JSON via `System.Text.Json`.

Don't "fix" code toward Microsoft conventions, and don't reformat per this repo
over the skill — the skill wins unless a repo file explicitly disagrees.

The skill's `references/` are built for grep, not full reads: list sections with
`grep -n "^## " <file>`, find one via `Keywords:`, and check `Trap:` bullets for
known mistakes.

## Commands

```shell
docker run --name hattori-postgres      -p 5432:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres:17-alpine
docker run --name hattori-postgres-test -p 5433:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres:17-alpine
dotnet tool restore
dotnet build            # must be warning-free
dotnet test             # needs the 5433 test container
```

Run the API from `Hattori.Web`:

```shell
cd Hattori.Web
dotnet ef database update --project ../Hattori.Persistence
dotnet watch            # http://localhost:5000; API docs at /scalar/v1 in development
```

A migration is added from `Hattori.Web`:
`dotnet ef migrations add <Name> --project ../Hattori.Persistence` — read it before committing.

Single test: `dotnet test --filter "FullyQualifiedName~TodoListRepositoryTests"`.
Tools (`Hattori.Tools`): `dotnet run -- roles seed`, `users create-admin --email … --password …`, `db seed-demo --email … --password …`, `db check`.

## Structure and rules that must not break

- `Hattori.Core` (entities, DTOs, interfaces, `Configurations/`, `Logger/`) knows nothing of EF Core; `Hattori.Persistence` holds every EF package, the `DbContext`, migrations, repositories and the audit interceptor; `Hattori.Web` holds controllers and `Program.cs`; references point one way (`Web → Persistence → Core`).
- Controllers depend on repository interfaces from `.Core`, never the `DbContext`. API controllers, never minimal APIs. Routes are `api/` + plural kebab-case.
- Every endpoint has `[Authorize]` and a permission policy from `Permissions.ALL`, unless deliberately public (`/health`, `api/account/register`). Permissions ride as `permission` claims added to the cookie by `HattoriUserClaimsPrincipalFactory`.
- Every setting is a `HATTORI_` environment variable read once in a `Configurations/` class; debug builds fall back with a warning, release builds throw at startup. Never call `Environment.GetEnvironmentVariable` anywhere else. Dev values go in `Hattori.Web/Properties/launchSettings.json` only.
- Never `Remove()` a soft-deletable entity: set `IsDeleted`/`DeletedAt`. Audit fields come from `AuditInterceptor`, never by hand.
- Moments are UTC (`DateTime.UtcNow`, `timestamp with time zone`); due dates are `DateOnly`. `DateTime.Now`/`TimeZoneInfo.Local` stay out.
- Tests run against real PostgreSQL on port 5433, not the EF in-memory provider.

## Skill management

- `.agents/skills/` is the canonical copy; `.claude/skills/helloyeew-dotnet-skill`
  is a symlink to it. Both are managed by the `skills` CLI and pinned by hash in
  `skills-lock.json`.
- Don't hand-edit the installed skill. Changes belong upstream in
  `HelloYeew/dotnet-skill`; refresh here with `npx skills check` / `npx skills update`.
