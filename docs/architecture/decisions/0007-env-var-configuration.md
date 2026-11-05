# ADR 0007: Settings are environment variables, not `appsettings.json`

- Status: Accepted
- Date: 2026-11-05

## Context

The app needs configuration (connection string, frontend origins, log timezone). The framework-default answer is `appsettings.json` + `IOptions<T>`, but these apps deploy into containers where values change per environment, and a half-configured app should fail loudly at startup, not misbehave on the first request that needs the value.

## Decision

Every setting is an environment variable `HATTORI_<NAME>`, read exactly once by `EnvironmentVariables.Required(name, debugDefault)` in a static `Configurations/` class with `Lazy` caching. Debug build: missing → logged warning + default. Release build: missing → throw before the app starts; `Program.cs` touches every required setting up front. Fixed-choice values parse into enums. `appsettings.json` keeps only sections ASP.NET or a library reads itself (`Logging`, `AllowedHosts`). Development values live in `Properties/launchSettings.json` — committed, but development-only by rule.

## Consequences

- Same artifact across environments; configuration mistakes stop the boot with a message naming the missing variable.
- One reader for `Environment.GetEnvironmentVariable` in the entire codebase; every setting is discoverable from one folder and one README table.
- Cost: no hot-reload of settings (restart required), and the types are static rather than injected `IOptions<>` — fine for values that change per deployment, which is exactly what these are.
