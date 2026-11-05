# ASP.NET Core

Personal preferences for web backends on ASP.NET Core, usually an API for a separate frontend, with PostgreSQL, plus Redis and background workers when the project needs them.

- General .NET rules, the configuration rule and the library list: `references/project-setup.md`. Code style: `references/csharp-style.md`.
- Real files start with the license header from `.editorconfig`. The snippets here leave it out.
- Examples use the placeholder project `Sample` (`Sample.Core`, `Sample.Web`, `SAMPLE_…`). Replace it with the real project name.

## Contents

- How to search this file
- Rules that apply everywhere
- Lay out the solution — `.Core`, `.Persistence`, `.Web`, `.Web.Tests`, `.Tools`
- Configure with environment variables — `<PROJECTNAME>_…`, `Configurations/`, `launchSettings.json`
- Log — Serilog, `ILogger<T>`, OpenTelemetry
- Write an API endpoint — `[ApiController]`, `ControllerBase`, `ActionResult<T>`, DTO records, `[EndpointDescription]`
- Access the database — repositories, `DbContext`, EF Core, PostgreSQL
- Soft delete and audit fields — `ISoftDeletable`, `IAuditable`, `HasQueryFilter`, `SaveChangesInterceptor`
- Change the schema — migrations, `dotnet-ef` as a local tool
- Handle time and time zones — UTC moments, wall-clock rules with a zone ID, `TimeZoneInfo`, `DateOnly`
- Sign in and check permissions — Identity, permission policies, cookies for a separate frontend
- Call other services over HTTP — `IHttpClientFactory`, typed clients, resilience pipelines
- Run background work — `BackgroundService`, `IServiceScopeFactory`, DI lifetimes
- Push live updates to the browser — SignalR hubs
- Use Redis — `IConnectionMultiplexer`, cache, streams, locks
- Handle errors — `AddProblemDetails`, `UseExceptionHandler`
- Report health — `AddHealthChecks`, `MapHealthChecks`
- Store uploaded files — `IImageService`, storage provider setting
- Test — NUnit against a real PostgreSQL test database
- Run it locally and in Docker — `dotnet watch`, Dockerfile, deployment checklist
- Write command-line tools — `<ProjectName>.Tools`, System.CommandLine
- Document the project — README, `docs/`, `CLAUDE.md`
- Packages — the ASP.NET library list

## How to search this file

Keywords: search, grep, find, index, contents, trap

Every `##` section is one ability. Its heading names the main APIs, and its first line starts with `Keywords:` followed by API names and plain words you might search for. Bullets starting with `Trap:` are known mistakes.

```shell
grep -n "^## " references/aspnet.md                     # list every ability
grep -n "Keywords:.*cookie" references/aspnet.md        # find the section for a word
grep -n "IHttpClientFactory" references/aspnet.md       # every mention of an API
grep -n "Trap:" references/aspnet.md                    # every known mistake
```

## Rules that apply everywhere

Keywords: rules, must, always, never, checklist, code review

1. API controllers, never minimal APIs.
2. Data goes through repositories. Controllers and services depend on interfaces from `.Core`, never on the `DbContext`.
3. Every setting is an environment variable named `<PROJECTNAME>_…`, read once and cached in a `Configurations` class, as `references/project-setup.md` says. Missing in a release build means the app doesn't start.
4. Every endpoint needs `[Authorize]` and a permission policy, unless it is deliberately public.
5. Log through `ILogger<T>` in web code, and always pass the exception: `logger.LogError(ex, "Saving failed")`.
6. Moments are stored in UTC (`DateTime.UtcNow`, `timestamp with time zone`). Rules on wall-clock time (schedules, time windows, "this month") are stored with their zone ID and evaluated in that zone. See "Handle time and time zones".
7. Never `Remove()` a soft-deletable entity. Set `IsDeleted` and `DeletedAt`.
8. HTTP calls go through `IHttpClientFactory`, with an explicit timeout.
9. All EF Core packages live in `.Persistence`. `.Core` references no EF package.

## Lay out the solution — `.Core`, `.Persistence`, `.Web`, `.Web.Tests`, `.Tools`

Keywords: project layout, solution structure, new web project, Core, Persistence, Web, Tools, Tests, project references, dependency direction, folder names, Entities, DTOs, Interfaces, Repositories

Name preference: `<ProjectName>.Web` for the web project, e.g. `Sample.Web`.

```
--- sample
 |- sample.slnx
 |- Sample.Core             # entities, DTOs, enums, interfaces, Configurations, Logger, domain logic
 |- Sample.Persistence      # DbContext, migrations, repositories, interceptors, EF implementations of services
 |- Sample.Web              # controllers, external-service implementations, background services, hubs, Program.cs
 |- Sample.Web.Tests        # NUnit tests
 |- Sample.Tools            # command-line tools: create admin, seed data, maintenance
 |- docs                    # design notes and how-tos
 |- README.md
 |- CLAUDE.md
```

References only point one way:

```
Sample.Web ──► Sample.Persistence ──► Sample.Core
     └─────────────────────────────────►┘
Sample.Tools ──► Sample.Persistence, Sample.Core
```

- `.Core` doesn't know `.Persistence` or EF Core exists. `.Persistence` references `.Core` for the entities and interfaces it implements. To change database provider, change `.Persistence` only.
- Folders in `.Core`: `Entities/`, `DTOs/`, `Enums/`, `Interfaces/` (repository and service interfaces), `Configurations/` (settings classes), `Logger/`, `Development/`.
- Folders in `.Persistence`: the `DbContext` at the root, `Migrations/`, `Repositories/`, `Interceptors/`.
- Folders in `.Web`: `Controllers/`, `Services/` (implementations of external services, background services), `Authorization/`, `Hubs/`.
- All DTOs live in `.Core`, so tests and every other project can use them.
- Trap: an entity that inherits `IdentityUser` pulls Identity into `.Core`. Reference `Microsoft.Extensions.Identity.Stores` there, which has no EF dependency. `Microsoft.AspNetCore.Identity.EntityFrameworkCore` in `.Core` brings EF Core in with it.

## Configure with environment variables — `<PROJECTNAME>_…`, `Configurations/`, `launchSettings.json`

Keywords: configuration, settings, environment variable, envvar, secret, connection string, launchSettings, appsettings, default value, fail at startup, encryption key, openssl

The rule is in `references/project-setup.md` (configuration with environment variables). In short:

- Name: `<PROJECTNAME>_<NAME>`, e.g. `SAMPLE_FRONTEND_BASE_URL`. Standard variables keep their own names (`ASPNETCORE_ENVIRONMENT`, `OTEL_EXPORTER_OTLP_ENDPOINT`).
- Read each one once, cache it in a static class in `.Core/Configurations/`, and use that class everywhere. Never call `Environment.GetEnvironmentVariable` anywhere else.
- Missing in a debug build: use the default and log a warning. Missing in a release build: throw, so the app doesn't start.
- A setting with a legitimate fallback (like the log timezone) logs a warning when it falls back.
- Use an enum whenever the value has fixed choices, so a typo fails loudly.

One helper keeps every settings class on the same rule:

```csharp
public static class EnvironmentVariables
{
    /// <summary>
    /// Reads a variable the app can't run without. A debug build falls back to <paramref name="debugDefault"/>
    /// with a warning; a release build throws.
    /// </summary>
    public static string Required(string name, string debugDefault)
    {
        string? value = Environment.GetEnvironmentVariable(name);

        if (!string.IsNullOrWhiteSpace(value))
            return value;

        if (!DebugUtils.IsDebugBuild)
            throw new InvalidOperationException($"{name} environment variable is not set.");

        Log.Warning("{EnvironmentVariableName} environment variable is not set. Using default value. (Default: {DefaultValue})", name, debugDefault);
        return debugDefault;
    }
}

public static class FrontendSettings
{
    // Lazy keeps the first result, including a thrown exception, so the variable is read once.
    private static readonly Lazy<string> base_url = new Lazy<string>(() => EnvironmentVariables.Required("SAMPLE_FRONTEND_BASE_URL", "http://localhost:3000"));

    public static string BaseUrl => base_url.Value;
}
```

Read every required setting at the top of `Program.cs`, so a missing one stops startup instead of failing the first request that needs it:

```csharp
_ = DatabaseSettings.DatabaseConnectionString;
_ = FrontendSettings.BaseUrl;
```

- Development values go in `Properties/launchSettings.json` under `environmentVariables` (see `references/project-setup.md`).
- Trap: `launchSettings.json` is committed. Development values only, never a real secret or a production key.
- `appsettings.json` holds only sections that ASP.NET or a library reads itself (`Logging`, `AllowedHosts`, the `Serilog` section the logger setup reads). Anything your own code reads is an environment variable.
- If the app encrypts data, use one key per purpose, so rotating one doesn't touch the others. Generate keys with `openssl rand -base64 32`.

## Log — Serilog, `ILogger<T>`, OpenTelemetry

Keywords: logging, log, Serilog, log file, rotation, retention, log level, timezone, GMT, UTC, OpenTelemetry, OTLP, traces, metrics, Sentry, privacy, GDPR, ILogger, exception, stack trace

Serilog, configured once in `.Core/Logger/` and called at the top of `Program.cs`:

```csharp
LoggerSetup.Configure(builder.Environment.IsDevelopment(), serviceName: "Sample.Web");
LoggerSetup.LogStartupHeader();

builder.Host.UseSerilog();
```

What the setup does:

- Writes to the console and to a rolling file: a new file each day or at 50 MB, kept for 14 days.
- Information level by default (Debug in development), `Microsoft.AspNetCore` at Warning, and EF Core's SQL chatter filtered out. A `Serilog` section in `appsettings.json` may override the levels and the file settings; a value that doesn't parse keeps its default instead of stopping startup.
- Timestamps in UTC with a label, e.g. `2026-01-01 12:00:00 GMT [INF]: Hosting starting`. Local time only when `<PROJECTNAME>_LOG_TIMEZONE` is set to an IANA ID (`Europe/Berlin`); an invalid ID falls back to UTC with a warning.
- Logs a short header at startup: app name, environment, version, start time and zone, OS, CPU count, .NET version and the log file's location. The top of any log file then says what wrote it.
- Logs unhandled exceptions as Fatal and flushes before the process ends.
- Sends logs to OpenTelemetry only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, with `service.name`, `service.version` and `deployment.environment` on every record.

A sketch of the core:

```csharp
public static class LoggerSetup
{
    private const string output_template = "{LogTimestamp} [{Level:u3}]: {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Configures Serilog: console, rolling file, and OpenTelemetry when an OTLP endpoint is set.
    /// </summary>
    public static void Configure(bool isDevelopment, string serviceName)
    {
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(isDevelopment ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Filter.ByExcluding(e => e.Properties.TryGetValue("SourceContext", out var source)
                                     && source.ToString().Contains("Microsoft.EntityFrameworkCore"))
            .Enrich.With(new LabelledTimestampEnricher(LogSettings.TimeZone))   // adds {LogTimestamp}
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: output_template)
            .WriteTo.File("logs/log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 50L * 1024 * 1024,
                retainedFileTimeLimit: TimeSpan.FromDays(14),
                outputTemplate: output_template);

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            configuration.WriteTo.OpenTelemetry(options => options.ResourceAttributes["service.name"] = serviceName);

        Log.Logger = configuration.CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "An unhandled exception occurred");
            Log.CloseAndFlush();
        };
    }
}
```

- `LabelledTimestampEnricher` is a small `ILogEventEnricher` of your own: it converts the event's timestamp to the log zone and adds it as `LogTimestamp`, formatted `yyyy-MM-dd HH:mm:ss` plus the zone label ("GMT" for UTC). `LogSettings.TimeZone` reads `<PROJECTNAME>_LOG_TIMEZONE` once, like any other setting.
- Rotate and retain log files, as above. A log folder that grows forever fills the disk eventually.
- Filter out namespaces that are too verbose (EF Core above), or raise their level in the `Serilog` overrides.
- On an error, log the full exception rather than its message, and include every detail that helps find the cause.
- OpenTelemetry is better than files alone, and Sentry is better still if the project has a budget for it. Check the project's privacy obligations (GDPR or the local equivalent) before sending anything to Sentry.
- In ASP.NET code, inject `Microsoft.Extensions.Logging.ILogger<T>` rather than calling Serilog directly, so the logging provider can change later. `builder.Host.UseSerilog()` already routes `ILogger<T>` through Serilog.
- Traces and metrics go to the same collector:

```csharp
const string service_name = "Sample.Web";
string appVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "0.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(service_name, serviceVersion: appVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

- Logs reach OTLP through Serilog. Don't add an OpenTelemetry logging provider as well: `UseSerilog()` replaces the logging providers, so it would never be written to.
- Keep the OpenTelemetry collector configuration in the repository with the code, so a deployment never depends on a config that lives only on a server.

## Write an API endpoint — `[ApiController]`, `ControllerBase`, `ActionResult<T>`, DTO records, `[EndpointDescription]`

Keywords: controller, endpoint, route, REST, API, GET, POST, PUT, DELETE, minimal API, MVC, DTO, record, validation, status code, NotFound, Conflict, CreatedAtAction, OpenAPI, Scalar, Swagger, API docs, .http file

Don't use minimal APIs. Use controllers, which scale better in a large project. A web project is usually an API for a separate frontend, so controllers derive from `ControllerBase` (no views).

```csharp
[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagRepository tags;

    public TagsController(ITagRepository tags)
    {
        this.tags = tags;
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.Tags.READ)]
    [EndpointDescription("Get a single tag by ID.")]
    public async Task<ActionResult<TagDto>> GetTag(Guid id, CancellationToken cancellationToken)
    {
        TagDto? tag = await tags.GetAsync(id, cancellationToken);

        if (tag == null)
            return NotFound();

        return tag;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Tags.WRITE)]
    [EndpointDescription("Create a tag. Names are unique, case-insensitive and stored in lowercase.")]
    public async Task<ActionResult<TagDto>> CreateTag(CreateTagDto dto, CancellationToken cancellationToken)
    {
        string name = dto.Name.Trim().ToLowerInvariant();

        if (await tags.ExistsAsync(name, cancellationToken))
            return Conflict($"Tag '{name}' already exists.");

        TagDto created = await tags.CreateAsync(name, dto.Color, cancellationToken);
        return CreatedAtAction(nameof(GetTag), new { id = created.Id }, created);
    }
}
```

- Routes: `api/` plus a plural, kebab-case resource (`api/tags`, `api/order-items`).
- The controller depends on repository and service interfaces only (see "Access the database").
- Take a `CancellationToken` and pass it down, so a request the client abandons stops querying.
- Request and response bodies are DTO records in `.Core/DTOs/`. Never return an entity: it leaks columns (password hashes, internal flags) and ties the API to the schema.
- Validate with data annotations on the DTO (`[Required]`, `[StringLength]`, `[Range]`). `[ApiController]` answers an invalid body with 400 before the action runs.
- Answer with the status helpers: `NotFound()`, `Conflict("…")`, `BadRequest("…")`, `CreatedAtAction(…)`, `NoContent()`.
- `[EndpointDescription("…")]` on every action. It becomes the OpenAPI description the frontend developers read.
- OpenAPI and its UI in development only:

```csharp
builder.Services.AddOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

- Keep a `<ProjectName>.Web.http` file with example requests for trying endpoints from Rider or VS Code.
- Trap: one controller that injects the `DbContext` "just this once" becomes the example the next controllers copy. Don't make the exception.

## Access the database — repositories, `DbContext`, EF Core, PostgreSQL

Keywords: database, EF Core, Entity Framework, PostgreSQL, Npgsql, repository, IRepository, service layer, DbContext, query, LINQ, projection, Dapper, legacy database, reverse engineer, scaffold, unit of work, SaveChanges, AsNoTracking

New project:

- EF Core with PostgreSQL from the start, or another database if the project needs it.
- Put the `DbContext`, interceptors, migrations and repositories in `<ProjectName>.Persistence`. Every EF Core `PackageReference` belongs there too, so changing database provider means changing `.Persistence` only.
- Entities live in `<ProjectName>.Core/Entities/`.
- Repositories: the interface in `.Core/Interfaces/` (`IUserRepository`), the EF implementation in `.Persistence/Repositories/` (`UserRepository`).
- Services with business logic: the interface in `.Core/Interfaces/` (`IUserService`), the implementation with EF Core in `.Persistence`. This keeps them easy to fake in tests. DI injects the right implementation.
- Use repositories as much as possible. Controllers and services never touch the `DbContext`.

```csharp
// Sample.Core/Interfaces/ITagRepository.cs
public interface ITagRepository
{
    Task<TagDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken);

    Task<TagDto> CreateAsync(string name, string color, CancellationToken cancellationToken);
}

// Sample.Persistence/Repositories/TagRepository.cs
public class TagRepository : ITagRepository
{
    private readonly SampleDbContext context;

    public TagRepository(SampleDbContext context)
    {
        this.context = context;
    }

    public Task<TagDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<Tag>()
               .Where(t => t.Id == id)
               .Select(t => new TagDto(t.Id, t.Name, t.Color))
               .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken) =>
        context.Set<Tag>().AnyAsync(t => t.Name == name, cancellationToken);

    public async Task<TagDto> CreateAsync(string name, string color, CancellationToken cancellationToken)
    {
        var tag = new Tag
        {
            Name = name,
            Color = color
        };

        context.Set<Tag>().Add(tag);
        await context.SaveChangesAsync(cancellationToken);

        return new TagDto(tag.Id, tag.Name, tag.Color);
    }
}
```

- Read methods may project straight into DTOs (DTOs live in `.Core`, so the interface can return them). That loads only the columns the caller needs. Use `AsNoTracking()` when returning entities nobody will change.
- A method that changes data saves its own change. When several changes must commit together (an order and its lines), expose `SaveChangesAsync` on the repository or use a transaction inside one repository method.
- Register the `DbContext`, repositories and EF services in one extension method in `.Persistence`, so `Program.cs` makes one call:

```csharp
public static IServiceCollection AddSamplePersistence(this IServiceCollection services, DatabaseConnectionTarget target)
{
    services.AddSampleDbContext(target);
    services.AddScoped<ITagRepository, TagRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();
    return services;
}
```

- Repositories and anything else that holds the `DbContext` are scoped.

Old project (the database already exists):

- For raw SQL, use Dapper.
- EF Core can ["reverse engineer"](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) an existing database into entities and a `DbContext`. It's not recommended for complex types and relationships. Read the documentation first.

## Soft delete and audit fields — `ISoftDeletable`, `IAuditable`, `HasQueryFilter`, `SaveChangesInterceptor`

Keywords: soft delete, IsDeleted, DeletedAt, query filter, IgnoreQueryFilters, audit, CreatedById, UpdatedById, UpdatedAt, interceptor, who changed this, hard delete

- Major entities implement `ISoftDeletable` (`IsDeleted`, `DeletedAt`). Delete by setting `IsDeleted = true` and `DeletedAt = DateTime.UtcNow`. Never call `Remove()` on them.
- The `DbContext` gives each of them a global filter, so deleted rows are invisible by default: `builder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);`. Use `.IgnoreQueryFilters()` only when you really need deleted rows, such as a uniqueness check or an admin audit view.
- Tables that are hard-delete only (logs, child rows that cascade) have no filter. Say so in a comment next to the filters.
- Entities implementing `IAuditable` (`CreatedById`, `UpdatedById`, `UpdatedAt`) are stamped by a `SaveChangesInterceptor` from the current user (an `AuditInterceptor` that gets `IHttpContextAccessor`). Never set these fields by hand.
- Register an interceptor that needs DI as a singleton and add it in `AddDbContext((serviceProvider, options) => …)`. `IHttpContextAccessor` is safe inside a singleton. Call `builder.Services.AddHttpContextAccessor()` before registering the `DbContext`.

## Change the schema — migrations, `dotnet-ef` as a local tool

Keywords: migration, schema change, add column, dotnet ef, dotnet-ef, database update, Up, Down, rollback, migration bundle, deploy migrations, dotnet-tools.json

- Pin `dotnet-ef` as a local tool in `dotnet-tools.json` at the solution root, so everyone runs the same version: `dotnet tool restore`.
- Create and apply a migration from the web project folder:

```shell
cd Sample.Web
dotnet ef migrations add AddTags --project ../Sample.Persistence
dotnet ef database update --project ../Sample.Persistence
```

- Read the generated migration before committing it. Every migration has both `Up()` and `Down()`.
- Use PostgreSQL-native column types: `jsonb`, `uuid`, `timestamp with time zone`, `character varying(n)`.
- Store moments as UTC in `timestamp with time zone`, written from `DateTime.UtcNow`; dates without a time as `date` (`DateOnly`). See "Handle time and time zones".
- Decide the production migration step before the first deployment. The usual choice is a migration bundle built in the pipeline (`dotnet ef migrations bundle --project ../Sample.Persistence`) and run before the new version starts.
- Trap: applying migrations by hand in production works until someone forgets one. Automate it.

## Handle time and time zones — UTC moments, wall-clock rules with a zone ID, `TimeZoneInfo`, `DateOnly`

Keywords: time zone, timezone, GMT, UTC, offset, daylight saving, IANA zone ID, local time, DateTime, DateTimeOffset, DateOnly, TimeOnly, DateTime.Now, TimeProvider, schedule, cron, time window, opening hours, business hours, report by day, month boundary, today, frontend display, ISO 8601, culture, calendar, non-Gregorian calendar

Tell three kinds of value apart, because each is stored and converted differently:

| Kind of value | Examples | Store as | Converted |
| --- | --- | --- | --- |
| A moment something happened | created at, run started, log line | UTC in `timestamp with time zone`, from `DateTime.UtcNow` | only for display |
| A rule on wall-clock time | cron schedule, opening hours, a recurring time window, "remind me at 09:00" | the values as entered (cron text, `TimeOnly`, weekdays) plus the IANA zone ID the rule belongs to; never converted to UTC | evaluated in its own zone, when needed |
| A period on a local calendar | "today", a daily or monthly report, a deadline at midnight | computed when needed | local start and end in the relevant zone, turned into a UTC range for the query |
| A date with no time | birthday, holiday, contract end date | `DateOnly` (column type `date`) | never |

- A rule keeps its own zone ID, so it means the same thing wherever the server runs and whoever looks at it. Fill in a new rule's zone from the user's preference, or from an app-wide default setting for an app used in one place (read like any other setting, see "Configure with environment variables").
- Use an IANA zone ID (`Europe/Berlin`, `America/New_York`), never a fixed offset such as `+01:00`. The ID carries the zone's daylight-saving rules; a fixed offset is wrong for half the year wherever the clocks change.
- Log timestamps default to UTC, so they line up with other systems' logs (see "Log").

Evaluate a rule in its own zone:

```csharp
// An unknown ID throws TimeZoneNotFoundException: validate the ID when the rule is saved.
TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(rule.TimeZoneId);

// Next run of a cron schedule. Cronos takes the time in UTC and returns UTC.
DateTime? nextRunUtc = rule.Expression.GetNextOccurrence(DateTime.UtcNow, zone);

// Inside a recurring time window? Compare the stored local weekday and time with local now.
DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
bool inWindow = rule.Days.Contains(localNow.DayOfWeek) && rule.Covers(TimeOnly.FromDateTime(localNow));

// "October 2026" in that zone, as a UTC range for the query.
var localMonthStart = new DateTime(2026, 10, 1);
DateTime fromUtc = TimeZoneInfo.ConvertTimeToUtc(localMonthStart, zone);
DateTime toUtc = TimeZoneInfo.ConvertTimeToUtc(localMonthStart.AddMonths(1), zone);
```

Between the API and the frontend:

- Moments leave the API as ISO 8601 in UTC (`2026-10-12T03:00:00Z`). System.Text.Json writes the `Z` for a `DateTime` whose `Kind` is `Utc`, which is what Npgsql returns for `timestamp with time zone`.
- Rules leave as they were entered (`"22:00"`, the cron text) together with their zone ID. Don't convert them.
- Display moments in the viewer's zone (the browser's, or a user preference). A value that belongs to a place (a shop's opening hours, an event at a venue) is shown in that place's zone and labelled with it: `new Intl.DateTimeFormat(undefined, { timeZone: rule.timeZoneId, timeStyle: 'short' })`.
- Text the backend writes for people (emails, PDFs, exports) is formatted in the reader's or the data's zone, and says which zone it is.
- Pass `now` into time-dependent logic, or inject `TimeProvider`, so tests can fix the clock.
- Trap: `DateTime.Now`, `DateTime.Today` and `TimeZoneInfo.Local` use the machine's time zone, which is the local zone on a developer's laptop and UTC in a container. The code passes locally and is hours off in production. Keep them out of the codebase.
- Trap: Npgsql refuses to write a `DateTime` whose `Kind` is `Unspecified` to `timestamp with time zone`, and a `DateTimeOffset` must have offset 0. Build moments with `DateTime.UtcNow` or `DateTimeKind.Utc`.
- Trap: a `DateTime` with `Kind = Unspecified` is serialized without the `Z`, and the browser reads it as its own local time.
- Trap: a weekday-plus-time rule can't be moved to UTC by shifting only the time: a window early on one day can fall on the previous day in UTC, and daylight saving shifts it twice a year. Evaluated in UTC, a nightly job runs at the wrong local hour and a month starts hours early or late.
- Trap: some cultures format dates in a non-Gregorian calendar by default, in .NET (`new CultureInfo(...)`) and in browsers (`Intl`), for example `ar-SA` (Um Al-Qura) and `th-TH` (Buddhist era). Use the invariant culture and ISO 8601 for anything a machine reads (the API, CSV, file names), and pick the calendar explicitly in documents for people.

## Sign in and check permissions — Identity, permission policies, cookies for a separate frontend

Keywords: authentication, authorization, login, logout, Identity, MapIdentityApi, cookie, SameSite, CORS, roles, permissions, policy, Authorize, claims, disabled account, self-registration, SSO, OIDC, Azure AD, LDAP, rate limit, brute force

Identity, with its API endpoints under `/account`:

```csharp
builder.Services.AddIdentityApiEndpoints<SampleUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SampleDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<SampleUserClaimsPrincipalFactory>();

app.MapGroup("/account").MapIdentityApi<SampleUser>();
```

- When accounts are created by administrators only (an internal app), block public self-registration: answer `POST /account/register` with 404 from a small middleware placed after `UseCors`, and create accounts from an admin endpoint or `.Tools`.
- Permissions, not roles, on endpoints. Each permission is a constant (`Permissions.Tags.READ = "Tags.Read"`), every constant gets a policy, and roles carry permissions as role claims:

```csharp
var authBuilder = builder.Services.AddAuthorizationBuilder();

foreach (string permission in Permissions.All)
    authBuilder.AddPolicy(permission, policy => policy.AddRequirements(new PermissionRequirement(permission)));

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

  Then `[Authorize(Policy = Permissions.Tags.READ)]` on the action. The pieces:
  - `Permissions`: nested static classes of constants, plus `All` listing every one.
  - `PermissionRequirement : IAuthorizationRequirement`, holding one permission name.
  - `PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>`, which succeeds when the user has a claim for that permission.
  - `SampleUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<SampleUser, IdentityRole>`, which adds the permission claims of the user's roles when the cookie is issued.
- Cookie for a frontend on another origin:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }

    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".Sample.Auth";

    // An API answers 401/403, never a redirect to a login page.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Stops the cookie policy middleware from downgrading SameSite=None on plain HTTP.
builder.Services.AddCookiePolicy(options => options.MinimumSameSitePolicy = SameSiteMode.None);
```

- CORS allows the frontend's origin, read from a setting:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy(front_end_cors_policy, policy => policy
        .WithOrigins(FrontendSettings.BaseUrl)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
```

- For several frontends, make the setting a comma-separated list and split it. Never hard-code an origin in `Program.cs`: a development address gets committed and shipped.
- If accounts can be disabled, reject them on every request: put an `is_active` claim in the cookie, and have a middleware after `UseAuthentication` return 401 when it's `false`, without waiting for the security stamp to be re-checked.
- Rate-limit the login endpoints, per client address. Without it, passwords can be guessed without limit:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
});

app.UseRateLimiter();
app.MapGroup("/account").MapIdentityApi<SampleUser>().RequireRateLimiting("login");
```

  Behind a reverse proxy, configure forwarded headers first, or every client shares the proxy's address and one limit.
- SSO: OpenID Connect (`Microsoft.AspNetCore.Authentication.OpenIdConnect`).
- If users sign in through LDAP, retry connection failures but never a wrong password (see "Call other services over HTTP").
- Middleware order: `UseExceptionHandler` → `UseCors` → static files → `UseHttpsRedirection` → `UseCookiePolicy` → `UseAuthentication` → disabled-account check (if any) → `UseRateLimiter` → `UseAuthorization` → `MapControllers`.

## Call other services over HTTP — `IHttpClientFactory`, typed clients, resilience pipelines

Keywords: HttpClient, external API, third party, webhook, email, SMTP, typed client, named client, timeout, retry, Polly, Microsoft.Extensions.Resilience, backoff, 429, TLS, self-signed certificate, socket exhaustion

- Put the interface of an external service in `.Core` and its implementation in `.Web`, injected by DI. To change provider (another email service, say), write a new implementation of `IEmailService` and nothing else changes.
- Never `new HttpClient()`. Register a typed or named client, always with a timeout:

```csharp
builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>(client =>
{
    client.BaseAddress = new Uri(ExchangeRateSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});
```

- Retry transient failures with a resilience pipeline (`Microsoft.Extensions.Resilience`): a timeout, then a few retries with exponential backoff on 5xx, 408 and 429.

```csharp
builder.Services.AddResiliencePipeline<string, HttpResponseMessage>("ExchangeRateResilience", pipeline =>
{
    pipeline.AddTimeout(TimeSpan.FromSeconds(15));
    pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(response => (int)response.StatusCode >= 500
                                      || response.StatusCode == HttpStatusCode.RequestTimeout
                                      || response.StatusCode == HttpStatusCode.TooManyRequests)
    });
});
```

- Never retry an authentication failure. A retried wrong password can lock the account.
- Turning certificate checks off is only for a test system with a self-signed certificate: put it behind a setting, on its own named client, and log a warning when it's on.
- Long transfers (uploads of large files) need a longer timeout, set explicitly on their own client.
- Messages that must not be lost (email, webhooks) go through an outbox: write a delivery row in the same transaction as the change, and let a background service send it with retries.

## Run background work — `BackgroundService`, `IServiceScopeFactory`, DI lifetimes

Keywords: background job, hosted service, worker, scheduled task, cron, timer, polling loop, queue consumer, scope, scoped, singleton, transient, lifetime, captive dependency, Cronos

```csharp
public class DailyStatsWorker : BackgroundService
{
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory scopes;
    private readonly ILogger<DailyStatsWorker> logger;

    public DailyStatsWorker(IServiceScopeFactory scopes, ILogger<DailyStatsWorker> logger)
    {
        this.scopes = scopes;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // A new scope per pass: a fresh DbContext, and nothing tracked from the last pass.
                using var scope = scopes.CreateScope();
                var stats = scope.ServiceProvider.GetRequiredService<IStatsService>();
                await stats.RecomputeDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // One failed pass must not end the service.
                logger.LogError(ex, "Statistics pass failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
```

- Register with `builder.Services.AddHostedService<DailyStatsWorker>();`.
- A hosted service is a singleton, so it creates a new scope for every pass and resolves scoped services from it.
- Lifetimes: anything that touches the `DbContext` is scoped; stateless helpers (a hashing helper) are singletons; a client that opens its own connection per call is transient.
- Trap: a singleton that takes a scoped service in its constructor keeps one instance for the life of the app (a captive dependency). Take `IServiceScopeFactory` instead, as above.
- When users enter cron schedules, parse them with Cronos.

## Push live updates to the browser — SignalR hubs

Keywords: SignalR, hub, websocket, real-time, live update, push notification, collaboration, presence

- `builder.Services.AddSignalR();` and `app.MapHub<NotificationHub>("/hubs/notifications");`.
- Put `[Authorize]` on every hub, and permission policies on hub methods as on controller actions.
- State shared between hub connections (who is editing what) lives in a singleton service, not in the hub: a new hub instance is created for every call.

## Use Redis — `IConnectionMultiplexer`, cache, streams, locks

Keywords: Redis, StackExchange.Redis, cache, stream, queue, consumer group, worker, message, pub/sub, lock

- One `IConnectionMultiplexer` for the whole app, registered as a singleton.
- When another process does the work, send it on a Redis stream read through a consumer group, and read its results from a stream of their own in a background service.
- Build stream names in one helper and never type them inline.
- Every message crossing to another process has a contract fixture tested on both sides (see "Test").
- A lock across instances (one job at a time per resource): a Redis lock with an expiry, so a crashed holder can't block forever.

## Handle errors — `AddProblemDetails`, `UseExceptionHandler`

Keywords: error handling, exception, 500, ProblemDetails, error response, stack trace leak, global exception handler

```csharp
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
```

- An unhandled exception becomes a logged error and a `ProblemDetails` 500 response, without the stack trace leaking to the client in production. Add both lines from the start.
- Expected failures (not found, conflict, invalid input) aren't exceptions: return the status helper from the action.
- Log the exception object, never only `ex.Message` (rule 5).

## Report health — `AddHealthChecks`, `MapHealthChecks`

Keywords: health check, liveness, readiness, /health, container, Docker, Kubernetes, load balancer, monitoring, database check, Redis check

Containers and load balancers need an endpoint that says whether the app and its dependencies work.

```csharp
// Sample.Persistence, inside the registration method (needs Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore)
services.AddHealthChecks().AddDbContextCheck<SampleDbContext>("database");

// Sample.Web/Program.cs, if the app uses Redis
builder.Services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");

app.MapHealthChecks("/health").AllowAnonymous();
```

```csharp
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        this.redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            TimeSpan latency = await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Ping {latency.TotalMilliseconds:0} ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable", ex);
        }
    }
}
```

- `/health` answers 200 when every check passes and 503 otherwise. It reveals nothing sensitive, so it can allow anonymous access.

## Store uploaded files — `IImageService`, storage provider setting

Keywords: upload, file storage, image, avatar, wwwroot, static files, S3, blob storage, volume

- Put storage behind an interface in `.Core` (`IImageService`) with the provider chosen by an enum setting, so moving from local disk to object storage is a new implementation:

```csharp
switch (StorageSettings.GetImageStorageProvider())
{
    case StorageProvider.Local:
        builder.Services.AddSingleton<IImageService, LocalImageService>();
        break;

    case StorageProvider.S3:
        builder.Services.AddSingleton<IImageService, S3ImageService>();
        break;

    default:
        throw new ArgumentOutOfRangeException(nameof(StorageProvider), "Unknown storage provider.");
}
```

- Local storage serves files from `wwwroot` with `UseStaticFiles()`. Create the folder before the middleware starts: if `wwwroot` is missing at startup, static files return 404 even after it's created.
- In a container, mount the upload folder as a volume, or uploads disappear with the container.
- Keep uploads out of git (`.gitignore`).

## Test — NUnit against a real PostgreSQL test database

Keywords: test, unit test, integration test, NUnit, test database, Docker, PostgreSQL, Redis, fixture, fake, mock, contract test, WebApplicationFactory

Run a separate database (and Redis, if the app uses it) for tests, on other ports:

```shell
docker run --name sample-postgres-test -p 5433:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres:17
docker run --name sample-redis-test -p 6380:6379 -d redis:8
```

`<PROJECTNAME>_TEST_DATABASE_CONNECTION_STRING` points at it (`DatabaseSettings` in `references/project-setup.md`; the debug default is port 5433).

```csharp
[TestFixture]
public class TagRepositoryTests
{
    private DbContextOptions<SampleDbContext> dbOptions = null!;
    private SampleDbContext context = null!;
    private TagRepository repository = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // A helper in .Persistence that builds the options for the production or the test database.
        dbOptions = SampleDatabaseRegistration.BuildOptions(DatabaseConnectionTarget.Test);

        await using var setupContext = new SampleDbContext(dbOptions);
        await setupContext.Database.MigrateAsync();
    }

    [SetUp]
    public void SetUp()
    {
        context = new SampleDbContext(dbOptions);
        repository = new TagRepository(context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await context.Set<Tag>().IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.DisposeAsync();
    }

    [Test]
    public async Task CreatedTagCanBeRead()
    {
        TagDto created = await repository.CreateAsync("urgent", "#3A7BD5", CancellationToken.None);

        TagDto? read = await repository.GetAsync(created.Id, CancellationToken.None);

        Assert.That(read?.Name, Is.EqualTo("urgent"));
    }
}
```

- Test against real PostgreSQL, not the EF in-memory provider: it doesn't enforce constraints and behaves differently from Postgres.
- Migrate once per fixture, clean the tables you used after each test, and dispose the context.
- Build repositories, services and controllers directly with the test context. Replace external services (email, a third-party API, a worker) with a hand-written fake class implementing the `.Core` interface. Prefer hand-written fakes to a mocking library.
- Messages crossing to another process (Redis streams, worker APIs) get contract fixtures: a sample message in a file that both sides test against, so neither can change the shape alone (e.g. `Sample.Web.Tests/Contracts/`).
- To test the whole pipeline (routing, authentication, policies), use `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing`. `Program` is internal with top-level statements, so `.Web` needs `<InternalsVisibleTo Include="<ProjectName>.Web.Tests" />`.
- Delete the template's `UnitTest1.cs`.

## Run it locally and in Docker — `dotnet watch`, Dockerfile, deployment checklist

Keywords: run, local setup, Docker, docker run, Dockerfile, container, image, deploy, production, hot reload, dotnet watch, checklist, .dockerignore

Local setup:

```shell
docker run --name sample-postgres -p 5432:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres:17
docker run --name sample-redis -p 6379:6379 -d redis:8      # if the app uses Redis
dotnet tool restore
cd Sample.Web
dotnet ef database update --project ../Sample.Persistence
dotnet watch
```

`dotnet watch` runs with hot reload; `dotnet run` without.

Dockerfile (in `.Web`, built from the solution root):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Only when the solution has one: it changes how every project builds.
COPY ["Directory.Build.props", "./"]
COPY ["Sample.Core/Sample.Core.csproj", "Sample.Core/"]
COPY ["Sample.Persistence/Sample.Persistence.csproj", "Sample.Persistence/"]
COPY ["Sample.Web/Sample.Web.csproj", "Sample.Web/"]
RUN dotnet restore "Sample.Web/Sample.Web.csproj"
COPY . .
WORKDIR "/src/Sample.Web"
RUN dotnet publish "./Sample.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sample.Web.dll"]
```

- Copy every project file the web project references before `dotnet restore`, so the restore layer covers all of them and is cached until a project file changes. The generated template copies only the web project's.
- The image runs as the non-root `$APP_UID` user.
- Keep a `.dockerignore` (`bin`, `obj`, `.git`, `.env`, IDE folders).

Before the first production deployment:

- `ASPNETCORE_ENVIRONMENT=Production`.
- Every required `<PROJECTNAME>_…` variable is set. A missing one stops startup in a release build.
- Secrets and keys are new, not the debug defaults from `launchSettings.json`.
- Migrations run before the new version starts (see "Change the schema").
- `/health` is wired to the container platform or load balancer.
- If telemetry is collected, `OTEL_EXPORTER_OTLP_ENDPOINT` points at the collector.
- In a container, upload and log folders are volumes.

## Write command-line tools — `<ProjectName>.Tools`, System.CommandLine

Keywords: CLI, command line, console app, admin account, create user, seed data, demo data, maintenance, purge, System.CommandLine

- `<ProjectName>.Tools` is a console app referencing `.Core` and `.Persistence`, for jobs that shouldn't be web endpoints: creating the first administrator, seeding demo data, database checks, clearing caches.
- Build the command tree with System.CommandLine (`users create-admin`, `db seed-demo`, `cache clear`). With no arguments, show an interactive menu.
- It builds its own `DbContext` and Identity services without the web host, so it reads the same environment variables as the web app.
- Run with `cd Sample.Tools`, then `dotnet run -- users create-admin --email admin@example.com`.

## Document the project — README, `docs/`, `CLAUDE.md`

Keywords: documentation, README, setup guide, onboarding, knowledge base, design notes, CLAUDE.md, AGENTS.md, AI assistant instructions

- `README.md`: how to run it locally (the Docker commands, `dotnet tool restore`, migrations, `dotnet watch`), how to add a migration, the tools, and how to generate any secrets it needs.
- `docs/`: design notes and how-tos for the larger parts of the system. Link them from the README.
- `CLAUDE.md`: the stack, the project layout, the conventions and the rules that must never break, kept short and pointing into `docs/` for detail. Keep it true: when a convention changes, update it in the same pull request.

## Packages — the ASP.NET library list

Keywords: NuGet, package, library, dependency, which library, approved libraries

Use these before reaching for anything else. The general list (System.Text.Json, NUnit, ImageSharp 3.x, JetBrains Annotations) is in `references/project-setup.md`.

| Need | Package | Project |
| --- | --- | --- |
| PostgreSQL with EF Core | `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` | `.Persistence` |
| Users and sign-in | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `.Persistence` (`Microsoft.Extensions.Identity.Stores` in `.Core`) |
| SSO | `Microsoft.AspNetCore.Authentication.OpenIdConnect` | `.Web` |
| LDAP | `Novell.Directory.Ldap.NETStandard` | `.Web` |
| Logging | `Serilog`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Serilog.Sinks.OpenTelemetry` (`.Core`); `Serilog.AspNetCore` (`.Web`) | |
| Traces and metrics | `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `.Http`, `.Runtime` | `.Web` |
| API documentation | `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore` | `.Web` |
| Retries and timeouts | `Microsoft.Extensions.Resilience` | `.Web` |
| Redis | `StackExchange.Redis` | `.Core` |
| Email | `MailKit` | `.Web` |
| Cron schedules | `Cronos` | where schedules are evaluated |
| Command-line tools | `System.CommandLine` | `.Tools` |
| Tests | `NUnit`, `NUnit3TestAdapter`, `NUnit.Analyzers`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, `Microsoft.AspNetCore.Mvc.Testing` | `.Web.Tests` |
