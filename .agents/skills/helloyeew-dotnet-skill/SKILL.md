---
name: helloyeew-dotnet-skill
description: HelloYeew's personal .NET and C# preferences — a C# style that deliberately differs from the usual .NET conventions (camelCase private methods, no _ prefix on fields, all_lower private constants, ALL_UPPER public constants, var everywhere except built-in types, XML docs on every member, no target-typed new() or collection expressions), plus project setup (.slnx, project layout, Directory.Build.props, .editorconfig, environment-variable configuration, library choices), ASP.NET Core web backends, and Sakura Framework desktop applications and 2D games (drawables, DI, screens, shaders, iPhone/iPad/Android, gameplay timing). Use this skill whenever writing, reviewing, refactoring or scaffolding C# or .NET code, creating a solution or csproj, setting up .editorconfig, building an ASP.NET Core API, or working with Sakura Framework, even if the user doesn't mention style or preferences.
---

# HelloYeew's .NET and C# preferences

These are HelloYeew's personal preferences for .NET and C#. Several of them differ on purpose from what most .NET code looks like, so default habits will get them wrong. When this skill applies, follow these preferences over the usual .NET conventions.

If a repository already has its own `.editorconfig` or style rules that disagree, follow the repository for formatting, and keep the rest of these preferences where they don't conflict. Consistency inside one codebase matters more than any single rule.

## The preferences that make this code different

### In code

Check every piece of C# against this list. The details and the reasons are in `references/csharp-style.md`.

1. **Private members are camelCase, with no underscore.** Fields, properties and methods alike: `private int itemCount;`, `private void updateLayout()`. Anything not private (including `internal` and `protected`) is PascalCase.
2. **Constants and static readonly fields:** non-private `ALL_UPPER` (`public const int MAX_SIZE = 64;`), private and local `all_lower` (`private const int max_size = 64;`, `private static readonly TimeSpan retry_delay`).
3. **No `var` for built-in types:** `int count = 0;`, `string name = …;`. `var` for everything else: `var sprite = new Sprite();`.
4. **Never `this.`**, and always an explicit accessibility modifier, even `private`.
5. **Braces on their own line** for everything. `if` may drop braces, but the body then goes on the next line. `for` and `while` always have braces.
6. **Expression bodies (`=>`)** for any single-expression method, property, accessor, indexer or operator, kept on one line.
7. **Object and collection initializers put one member per line**, even short ones. Lines otherwise run up to 228 characters.
8. **Every class and method has an XML doc comment**, even a trivial one, with `<summary>` and `</summary>` on their own lines. Comments inside methods are rare: only for complex flows and gotchas.
9. **Usings are written out** (`ImplicitUsings` disabled) in Rider's order: `System.*` first, then the rest alphabetically ignoring letter case (`Sakura` before `SDL`), then `using static`, then aliases. An unused using is an error, so never leave one. Nullable is on, namespaces are file-scoped.
10. **Explicit `new` and classic constructors:** `new List<int> { 1, 2 }`, `new[] { a, b }`, `new Sprite()` (never target-typed `new()`), a constructor assigning fields. Don't convert code to primary constructors, collection expressions (`[ … ]`) or the `field` keyword just because the language allows it; Rider is set not to push them.
11. **American English** in names, comments and docs: `Color`, `Gray`, `Center`, `Initialize`. An existing API keeps its own spelling (Sakura's `Anchor.Centre`).
12. **Log the exception, not its message:** `logger.LogError(ex, "Saving failed")`, `Logger.Error("Saving failed", ex)`.

### In projects

Apply these when creating or changing a solution, a project or its configuration. The details are in `references/project-setup.md`.

1. **Solutions are `.slnx`**, never `.sln`, with every project in its own subfolder.
2. **No bare project names:** every project has a suffix that says what it is, even the shared one: `Sample.Core`, `Sample.Web`, `Sample.Web.Tests`.
3. **In a server or web backend, every setting is an environment variable** `<PROJECTNAME>_…`, read once in a `Configurations/` class. A Debug build may fall back to a default with a logged warning; a Release build throws at startup. A Sakura app doesn't do this: the settings the user changes are saved by `ConfigManager<TEnum>`.
4. **Libraries:** `System.Text.Json` (not Newtonsoft), NUnit (not xUnit), ImageSharp 3.x only, JetBrains Annotations. Pin exact package versions.

## Read the reference for the task

Read only the file the task needs. Each is organized so it can be searched (see below).

| Task | Read |
| --- | --- |
| Writing or reviewing any C# | `references/csharp-style.md` |
| New solution or project, csproj, `.editorconfig`, `Directory.Build.props`, license header, configuration, which library, Rider setup, versioning | `references/project-setup.md` |
| Web backend, REST API, database, auth, background jobs, Docker (ASP.NET Core) | `references/aspnet.md` |
| Anything on Sakura Framework: app class, drawables, DI, layout, input, screens, settings, textures, shaders, audio, tests, iPhone/iPad/Android | `references/sakura-framework.md` |
| A desktop tool on Sakura: solution layout, UI scaling, theming, image loading and processing, pan and zoom, desktop release | `references/sakura-application.md`, after the framework file |
| A 2D game on Sakura: solution layout, gameplay clock and timing, gameplay state, saving, replays, phone layout, effects, game tests, release | `references/sakura-game.md`, after the framework file |

On Sakura, before writing any helper, check Sakura's `Extensions/` folder (`ColorExtensions`, `DrawableExtensions`, `ObjectExtensions`, …) and `Utilities/` (`Precision`, …): almost everything you need is already there. Null checks are `.IsNull()` / `.IsNotNull()` (never `== null`), and float comparisons are `Precision.AlmostEquals` (never `==`).

Sakura Framework is an open-source cross-platform 2D framework for UI applications and games: https://github.com/HelloYeew/sakura. Its visual tests (`Sakura.Framework.Tests/Visuals/`) have a working example of almost every feature.

## Search the references instead of reading them whole

The framework references are long. Every `##` section is one ability, its first line is `Keywords:` with API names and plain words, and known mistakes start with `Trap:`.

```shell
grep -n "^## " references/sakura-framework.md                 # every ability in a file
grep -n "Keywords:.*drag" references/sakura-framework.md      # the section for a word
grep -rn "Trap:" references/                                  # every known mistake
grep -rn "FromPixelData" references/                          # every mention of an API
```

Then read just that section. Check the `Trap:` bullets of every section you use: each one is a known mistake.

## Starting a new project

1. Pick the name and the layout (`references/project-setup.md`, "Name the project", "Lay out the solution", "Name the projects inside it").
2. Add `.editorconfig`, `Directory.Build.props`, the `.licenseheader` file and `<project-name>.sln.DotSettings` from `references/project-setup.md`.
3. For an ASP.NET backend, add the `Configurations/` class and `launchSettings.json` before the first setting is needed. A Sakura app needs neither: its `Configurations/` folder holds a `ConfigManager<TEnum>` and the setting enum instead (`references/sakura-framework.md`, "Save user settings").
4. Then follow the framework reference: `references/aspnet.md`, "Lay out the solution", or `references/sakura-framework.md`, "Create a new Sakura app", plus the application or game reference.

## Reviewing code

Go through the code preferences above (and the project ones if the change touches a csproj, the solution or configuration), then the `Rules that apply everywhere` section of the framework reference in use. Call out style differences as style, and keep them separate from real bugs, so the important findings don't get lost among formatting notes.
