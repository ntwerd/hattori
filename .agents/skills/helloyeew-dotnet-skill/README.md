# helloyeew-dotnet-skill

A Claude Code skill with my personal .NET and C# preferences: the way I write C#, set up projects, and build things on ASP.NET Core and [Sakura Framework](https://github.com/HelloYeew/sakura).

This skill include all of my C# personal styling that's different from Microsoft's .NET convention (e.g. disable implicit using, not `var` anywhere, private constants are `all_lower` and public ones `ALL_UPPER`, mark unused usage as "error") to my project setup style and knowledge of some framework and my .NET ecosystem library preference I use for work like ASP.NET, especially Sakura Framework that I use it personally for some GUI desktop application to making game with it with some coding and styling recommendation and make sure you use all of Sakura's advantage on your project from setup, cross-platform code to release your app to installable file and best performance as possible.

## Install

Clone it into your Claude Code skills folder:

```shell
git clone https://github.com/HelloYeew/dotnet-skill ~/.claude/skills/helloyeew-dotnet-skill
```

Claude loads it when you work on C# or .NET code. Update with `git pull` in that folder.

## What's inside

| File | Covers |
| --- | --- |
| [`SKILL.md`](SKILL.md) | The preferences that make this code different, and which reference to read for a task |
| [`references/csharp-style.md`](references/csharp-style.md) | The C# style, rule by rule |
| [`references/project-setup.md`](references/project-setup.md) | Naming, solution layout, `.editorconfig`, `Directory.Build.props`, csproj, environment-variable configuration, libraries, Rider, versioning |
| [`references/aspnet.md`](references/aspnet.md) | Web backends on ASP.NET Core |
| [`references/sakura-framework.md`](references/sakura-framework.md) | Apps and games on Sakura Framework, including iPhone, iPad and Android |
| [`references/sakura-application.md`](references/sakura-application.md) | Desktop tools on Sakura |
| [`references/sakura-game.md`](references/sakura-game.md) | 2D games on Sakura |

The references are organized so they can be searched: every `##` section is one ability, starts with a `Keywords:` line, and lists known mistakes as `Trap:` bullets.

```shell
grep -n "^## " references/sakura-framework.md
grep -rn "Trap:" references/
```

You can read this by eye too. 👍
