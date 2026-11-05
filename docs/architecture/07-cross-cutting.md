# 07 · Cross-cutting concerns

## Configuration

Every setting is an environment variable named `HATTORI_<NAME>` (standard variables like `OTEL_EXPORTER_OTLP_ENDPOINT` keep their own names). The mechanism, not the table — the table lives in the [README](../../README.md#environment-variables):

- `EnvironmentVariables.Required(name, debugDefault)` is the only reader of environment variables in the codebase. Debug build: missing → logged warning + default. Release build: missing → `InvalidOperationException` at startup.
- Each settings class caches its value with `Lazy`, so every variable is read once per process (the register-endpoint logging stays quiet after startup).
- `Program.cs` touches every required setting before building the app (`_ = DatabaseSettings.DatabaseConnectionString; …`), so a misconfigured deployment stops at boot, not at the first request.
- `appsettings.json` holds only what ASP.NET itself reads (`Logging`, `AllowedHosts`). Anything the app's own code reads is an environment variable — [ADR 0007](decisions/0007-env-var-configuration.md).
- Development values live in `Properties/launchSettings.json`; never a real secret.

## Logging

`LoggerSetup.Configure(isDevelopment, serviceName)` runs before anything else in `Program.cs`, then `builder.Host.UseSerilog()` routes `ILogger<T>` through it.

- **Destinations:** console + rolling file `logs/log.txt` (daily, 50 MB roll, 14 days retention).
- **Levels:** Debug in development, Information otherwise; `Microsoft.AspNetCore` at Warning; EF Core's SQL chatter filtered out.
- **Timestamps:** the `LabelledTimestampEnricher` renders `2026-11-05 12:00:00 GMT [INF]: …` — UTC by default, the IANA zone from `HATTORI_LOG_TIMEZONE` when set (invalid ID → warning + UTC).
- **Startup header:** app name, version, environment, .NET version, OS, CPU count, log folder, run start — the top of every log file says what wrote it.
- **Crashes:** unhandled exceptions log Fatal and flush before the process ends.
- **Rule:** web code injects `ILogger<T>` and always passes the exception — `logger.LogError(ex, "Saving failed")` — never `ex.Message` alone.

## Telemetry

Only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (`TelemetrySettings.OtlpEndpointSet`): OpenTelemetry traces (ASP.NET Core) and metrics (ASP.NET Core, runtime, HTTP client) with `service.name = Hattori.Web` and the assembly version. Serilog logs reach the same collector via `Serilog.Sinks.OpenTelemetry`. With no endpoint, none of it registers.

## Error handling

Two lines from day one: `AddProblemDetails()` + `app.UseExceptionHandler()`. Consequences:

- Unhandled exception → logged (with the exception) + RFC 7807 `application/problem+json` 500, no stack trace leak.
- Expected failures are **not** exceptions: the controllers return `NotFound()`, `Conflict("…")`, `ValidationProblem(…)`, `CreatedAtAction(…)`, `NoContent()`.
- `/health` answers 200 when the database check (`AddDbContextCheck`) passes, 503 otherwise; it reveals nothing else and allows anonymous access.

## Time

Three kinds of value, kept apart everywhere:

| Kind | Example | Representation |
| --- | --- | --- |
| A moment | `CompletedAt`, `DeletedAt`, `UpdatedAt`, log lines | `DateTime.UtcNow` → `timestamp with time zone` → JSON with `Z` |
| A date with no time | `DueDate` | `DateOnly` → `date` → JSON `2026-11-06`; never converted |
| A display preference | log timestamp zone | IANA ID in `HATTORI_LOG_TIMEZONE`, evaluated where shown |

`DateTime.Now` and `TimeZoneInfo.Local` appear nowhere — code using them passes on a UTC-laptop dev machine and is hours off in production.

## API documentation

`builder.Services.AddOpenApi()`; in development only, `MapOpenApi()` + `MapScalarApiReference()` serve the document and the Scalar UI (at `/scalar/v1`). Every action carries `[EndpointDescription("…")]`, which becomes the OpenAPI description the frontend reads. `Hattori.Web.http` keeps runnable examples for Rider/VS Code.

## Build-wide conventions

Enforced by tooling, not review memory:

- `.editorconfig` makes public-member PascalCase an **error**, private-member camelCase, `all_lower` private constants and `ALL_UPPER` public constants/statics warnings — the compiler catches most style drift.
- `Directory.Build.props` pins `LangVersion` 14.0, enables `Nullable` and `GenerateDocumentationFile` for every project.
- `ImplicitUsings` is disabled per project: every file lists its usings, so type origins are always visible.
- Package versions are exact pins; `dotnet-ef` is pinned as a local tool.
- Every `.cs` file starts with the MIT license header from `.editorconfig`'s `file_header_template` (generated migrations excepted).

Next: [08 · Maintainers' guide](08-maintainers-guide.md).
