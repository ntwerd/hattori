# hattori

A todo application API service on ASP.NET Core, built the way [HelloYeew's .NET skill](.agents/skills/helloyeew-dotnet-skill/SKILL.md) prescribes: controllers (no minimal APIs), repositories over EF Core + PostgreSQL, full Identity with permission policies, Serilog, NUnit.

## Projects

| Project | What's inside |
| --- | --- |
| `Hattori.Core` | Entities, DTOs, interfaces, `Configurations/` settings classes, `Logger/` Serilog setup, permission constants |
| `Hattori.Persistence` | `HattoriDbContext`, migrations, repositories, the audit interceptor, the claims factory |
| `Hattori.Web` | Controllers, the permission authorization handler, `Program.cs` |
| `Hattori.Web.Tests` | NUnit tests against a real PostgreSQL test database |
| `Hattori.Tools` | CLI: create an administrator, seed roles, seed demo data, check the database |

References point one way: `Web → Persistence → Core`, `Web → Core`, `Tools → Persistence + Core`. All EF Core packages live in `.Persistence`.

## Prerequisites

- .NET SDK 10
- Docker, for the two PostgreSQL containers

## Run it locally

```shell
docker run --name hattori-postgres      -p 5432:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres:17-alpine
docker run --name hattori-postgres-test -p 5433:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres:17-alpine
dotnet tool restore
cd Hattori.Web
dotnet ef database update --project ../Hattori.Persistence
dotnet watch          # http://localhost:5000, API docs at /scalar/v1 (development only)
```

Try the endpoints from `Hattori.Web/Hattori.Web.http`.

## Add a migration

```shell
cd Hattori.Web
dotnet ef migrations add <Name> --project ../Hattori.Persistence
dotnet ef database update --project ../Hattori.Persistence
```

Read the generated migration before committing it.

## Environment variables

Every setting is an environment variable read once in `Hattori.Core/Configurations/`. A debug build falls back to a default with a logged warning; a release build refuses to start.

| Variable | What it configures | Debug default |
| --- | --- | --- |
| `HATTORI_DATABASE_CONNECTION_STRING` | The served database | `localhost:5432/hattori` |
| `HATTORI_TEST_DATABASE_CONNECTION_STRING` | The test database | `localhost:5433/hattori_test` |
| `HATTORI_FRONTEND_BASE_URL` | CORS origins, comma-separated | `http://localhost:3000` |
| `HATTORI_LOG_TIMEZONE` | IANA ID for log timestamps | UTC |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Standard OTLP variable; enables telemetry when set | unset |

Development values live in `Hattori.Web/Properties/launchSettings.json` — never a real secret.

## Tests

The tests run against real PostgreSQL on port 5433 (the second container above):

```shell
dotnet test
```

## Tools

```shell
cd Hattori.Tools
dotnet run -- roles seed
dotnet run -- users create-admin --email admin@example.com --password 'Passw0rd!'
dotnet run -- db seed-demo --email demo@example.com --password 'Passw0rd!'
dotnet run -- db check
```

Registration is open at `api/account/register` (rate-limited); the identity API's own `/account/register` answers 404.

## Docker

The Dockerfile is in `Hattori.Web` and builds from the solution root:

```shell
docker build -f Hattori.Web/Dockerfile -t hattori .
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production -e HATTORI_DATABASE_CONNECTION_STRING='Host=host.docker.internal;Database=hattori;Username=postgres;Password=mysecretpassword' -e HATTORI_FRONTEND_BASE_URL='https://example.com' hattori
```

Wire `/health` to the platform's checks; it answers 200 when the database check passes.
