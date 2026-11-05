# Sakura Framework

How I build apps and games on [Sakura Framework](https://github.com/HelloYeew/sakura). Sakura is freeform, so most things can be done several ways. This file records the way I use it and the traps I've already fallen into.

- Written against Sakura `2026.1101.0`. Where an API is newer than that, the section says so.
- Desktop tools: also read `references/sakura-application.md`. Games: also read `references/sakura-game.md`. Code style: `references/csharp-style.md`. General .NET setup: `references/project-setup.md`.
- API reference: https://helloyeew.github.io/sakura-api-docs. Working examples of almost every feature: the visual tests in `Sakura.Framework.Tests/Visuals/` in the Sakura repo.

## Contents

- How to search this file
- Rules that apply everywhere
- Use Sakura's extensions and utilities first
- Create a new Sakura app
- Work against a local Sakura checkout
- Write the App class
- Load bundled files
- Build a custom drawable
- Share services between drawables
- Size and place things
- Keep content clear of notches
- Draw shapes, text and icons
- Animate
- Handle mouse, keyboard and dropped files
- Show tooltips and change the cursor
- Use the built-in controls
- Show menus and right-click menus
- React to value changes
- Save user settings
- Move between screens
- Show panels, popups and dialogs
- Run slow work without freezing the UI
- Show pixels on screen
- Blur, grayscale and cached rendering
- Write a custom shader
- Show many items efficiently
- Open and save files
- Play sounds and video
- Log and handle errors
- Debug a running app
- Hot reload while you work
- Write tests
- Add iPhone, iPad and Android
- Handle phones
- Ship a release
- Not covered yet

## How to search this file

Keywords: search, grep, find, index, contents, trap

Every `##` section is one ability. Its heading names the main APIs, and its first line starts with `Keywords:` followed by API names and plain words you might search for. Bullets starting with `Trap:` are known mistakes.

```shell
grep -n "^## " references/sakura-framework.md                 # list every ability
grep -n "Keywords:.*drag" references/sakura-framework.md      # find the section for a word
grep -n "FromPixelData" references/sakura-framework.md        # every mention of an API
grep -n "Trap:" references/sakura-framework.md                # every known mistake
```

## Rules that apply everywhere

Keywords: rules, must, always, never, checklist, code review

1. Every `Drawable` subclass is `partial`, so the DI source generator can handle it.
2. Only the update thread changes drawables. Background work hands its result back with `Scheduler.Add`.
3. Only the draw thread creates or frees GPU objects, through `host.Renderer.ScheduleToDrawThread`.
4. A drawable subscribes to anything that outlives it (settings, shared services) with its own `BindValueChanged`, never `reactive.ValueChanged +=`.
5. `Cache()` a service before `Add()`-ing anything that resolves it.
6. Dispose everything you allocate: `Texture`, `ImageRawData`, `NativeMemoryLease`, and native objects from any image library you add (`MagickImage`, OpenCV's `Mat`).
7. Log the exception, not its message: `Logger.Error("Saving failed", ex)`.
8. Pin an exact Sakura version in the csproj. Never `*-*`.
9. Use what the framework already has: image and video decoding, audio, fonts, file dialogs, settings storage. Override a `Create…` hook or add a library only for a need you can name.
10. Check Sakura's `Extensions/` folder (`ColorExtensions`, `DrawableExtensions`, `ObjectExtensions`, …) and `Utilities/` (`Precision`, …) before writing any helper: almost everything you need is already there (see "Use Sakura's extensions and utilities first").
11. Null checks use `.IsNull()` / `.IsNotNull()`, never `== null` / `!= null`. Float and double comparisons use `Precision.AlmostEquals` (and its siblings), never `==`.

## Use Sakura's extensions and utilities first — `IsNull`, `IsNotNull`, `Precision.AlmostEquals`, `Extensions/`, `Utilities/`

Keywords: extension method, helper, utility, already exists, null check, IsNull, IsNotNull, AsNonNull, == null, != null, float compare, double compare, epsilon, tolerance, AlmostEquals, AlmostEqualZero, AlmostBigger, DefinitelyBigger, Precision, ColorExtensions, DrawableExtensions, TransformExtensions, retry

Before writing a helper, look in Sakura's `Sakura.Framework/Extensions/` and `Sakura.Framework/Utilities/`. Almost every small helper a Sakura app needs is already there: colors in `ColorExtensions`, animations and tree walks in `DrawableExtensions`, null checks in `ObjectExtensions`, float comparison in `Precision`. Using them keeps the code consistent with the framework.

Null checks: `.IsNull()` and `.IsNotNull()`, never `== null` or `!= null`:

```csharp
using Sakura.Framework.Extensions.ObjectExtensions;

if (texture.IsNull())
    return;

if (result.IsNotNull())
    preview.Show(result);
```

- They compare with `ReferenceEquals`, so an overloaded `==` can't change the answer, and they carry `[NotNullWhen]`, so the compiler knows the value isn't null afterwards, just as with `== null`.
- `obj.AsNonNull()` instead of the `!` null-forgiving operator, and only where an assertion or real handling isn't reasonable.

Float and double comparisons: `Precision`, never `==`:

```csharp
using Sakura.Framework.Utilities;

if (Precision.AlmostEquals(scale, 1f))
    return;

if (Precision.AlmostEqualZero(velocity))
    stop();
```

- `AlmostEquals` (float, double and `Vector2`), `AlmostEqualZero`, `AlmostBigger`, `DefinitelyBigger` and `AlmostIntersects` (`RectangleF`) all take an optional tolerance. The defaults are `Precision.FLOAT_EPSILON` (`1e-3f`) and `Precision.DOUBLE_EPSILON` (`1e-7`).
- Trap: `if (alpha == 0.5f)` after an animation or any arithmetic is almost never true. Layout, transforms and clocks all produce values that are close, not equal.

What else is there:

| Class (namespace `Sakura.Framework.Extensions.…` unless noted) | Main methods |
| --- | --- |
| `ObjectExtensions` | `IsNull`, `IsNotNull`, `AsNonNull` |
| `ColorExtensions` | `FromHex`, `ToHex`, `FromRgb(a)`, `FromHSL`/`ToHSL`, `FromHSV`/`ToHSV`, `Darken`, `Lighten`, `Lerp`, `Blend`, `BlendPerceptually`, `WithAlpha`, `WithHue`, `WithLightness`, `WithSaturation`, `Invert`, `ToGrayscale`, `IsDark`/`IsLight`, `GetBrightness`, `SrgbToLinear`, `SampleFromLinearGradient`, `GetRelativeLuminance`, `GetContrastRatio` |
| `DrawableExtensions` (transforms) | `FadeIn`, `FadeOut`, `FadeTo`, `FadeToColor`, `FlashColor`, `MoveTo`, `MoveToX`/`Y`, `MoveToOffset`, `ScaleTo`, `ResizeTo`, `RotateTo`, `Spin`, `Then`, `Wait`, `Loop`, `Schedule`, `AddDelayed`, `FadeBorderColorTo`, `FadeEdgeEffectTo` |
| `DrawableExtensions` (tree) | `Descendants`, `DescendantsAndSelf`, `CountOfType` |
| `ReactiveExtensions` | `BindBothWaysTo` |
| `IEnumerableExtensions` | `Yield` (one item as a sequence) |
| `ExceptionExtensions` | `AsSingular`, `Rethrow` |
| `DefaultFontWeightsExtensions`, `IconUsageExtensions` | `ToWeightValue`, `ToGlyph` |
| `Sakura.Framework.Extensions` | `CheckIsValidUrl`, `TrimDirectorySeparator` |
| `Sakura.Framework.Utilities.Precision` | `AlmostEquals`, `AlmostEqualZero`, `AlmostBigger`, `DefinitelyBigger`, `AlmostIntersects` |
| `Sakura.Framework.Utilities.General` | `AttemptWithRetryOnException<TException>` (retry an action a set number of times) |

- This list is from `2026.1101.0` (except `Descendants` and the contrast methods, which are newer). Check the folders themselves for anything added since.
- A helper that's general enough to need in two apps belongs in Sakura's `Extensions/`, not copied between apps.

## Create a new Sakura app — `dotnet new sakura`, `HostOptions`, `AppType`, `Directory.Build.props`

Keywords: new project, start, template, solution layout, slnx, csproj, Program.cs, Main, DesktopAppHost, HostOptions, NuGet version, Directory.Build.props, EmbeddedResource, InternalsVisibleTo, storage folder, portable, AppType, Game, Application, frame limiter, keep screen on, --ios, --android, .DS_Store

```shell
dotnet new install Sakura.Framework.Templates
dotnet new sakura -n Sample                    # add --ios and/or --android for phone heads
```

The template creates `Sample`, `Sample.Desktop` and `Sample.sln` (plus `Sample.iOS` and `Sample.Android` with the flags), and references `Sakura.Framework` with version `*-*` in both `Sample` and `Sample.Desktop`. Change it to one of my two layouts:

- A desktop tool: `Sample.Application`, `Sample.Desktop`, `Sample.Application.Tests`. See `references/sakura-application.md`, "Lay out an application solution".
- A game: `Sample.Game`, `Sample.Game.Desktop`, `Sample.Game.iOS`, `Sample.Game.Android`, `Sample.Game.Resources`, `Sample.Game.Tests`. See `references/sakura-game.md`.

In both:

- Migrate to `.slnx` (`dotnet sln migrate`, then delete the `.sln`).
- Only the shared project (`.Application` or `.Game`) references `Sakura.Framework`. Delete the reference the template puts in `.Desktop`: the other projects get it through their reference to the shared project. Phone heads add only their own head package (see "Add iPhone, iPad and Android").
- Pin the version: `<PackageReference Include="Sakura.Framework" Version="2026.1101.0" />`. Sakura uses CalVer, and `*-*` pulls whatever was published last.
- `Directory.Build.props` must embed resources, or nothing in `Resources/` can be loaded:

```xml
<ItemGroup Label="Resources">
  <EmbeddedResource Include="Resources\**\*.*" Exclude="Resources\**\.DS_Store" />
</ItemGroup>
```

`Program.cs` in `.Desktop` only starts the host:

```csharp
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using AppHost host = new DesktopAppHost("sample", new HostOptions { AppType = AppType.Application });
        using App app = new SampleApp();
        host.Run(app);
    }
}
```

- The host name (`"sample"`) is the name of the app's data folder: `~/Library/Application Support/sample` on macOS, `%LOCALAPPDATA%\sample` on Windows, `~/.local/share/sample` on Linux. Settings, logs and anything else in `Storage` live there.
- Trap: renaming it after a release makes every user lose their settings.
- `HostOptions.AppType` is `Game` unless you say otherwise. A game busy-spins the update and draw threads, runs input and audio at 1000 Hz, limits frames to twice the refresh rate, and keeps the screen awake. A tool sets `AppType = AppType.Application` (VSync, no spinning, the screen can sleep). `host.KeepScreenOn` can still be switched on while a video plays.
- Trap: settings are written with their defaults, so a machine that ran the app as a `Game` keeps `FrameLimiter = Limit2x` in its `framework.ini` after you switch to `Application`.
- `HostOptions.FriendlyAppName` is the display name. `PortableInstallation = true` keeps all data beside the executable instead (this also happens when a `framework.ini` sits beside it).
- The desktop app and the tests need the .NET 10 SDK. iOS needs the .NET 11 SDK.

## Work against a local Sakura checkout — `UseLocalFramework.sh`

Keywords: local framework, project reference, debug the framework, unreleased fix, framework change, source generator, analyzer

Clone Sakura next to the app (`sakura/` beside `sample/`), then from the app root run a `UseLocalFramework.sh` that swaps the NuGet reference for project references. A good script does four things:

- Replaces the `Sakura.Framework` package in the shared project with a project reference to `../sakura/Sakura.Framework/Sakura.Framework.csproj`.
- Also references `../sakura/Sakura.Framework.SourceGenerators/Sakura.Framework.SourceGenerators.csproj` with `ReferenceOutputAssembly="false" OutputItemType="Analyzer"`, so DI stays source-generated. The NuGet package brings the generator along; a project reference doesn't.
- Pins the framework's build configuration (`SetConfiguration="Configuration=Release"` on the reference), Release by default and Debug when you want to step into it.
- Points the phone heads at `Sakura.Framework.iOS` and `Sakura.Framework.Android` too, and imports their `buildTransitive` files: the iOS `.props` right after `<Project Sdk=…>`, the iOS and Android `.targets` just before `</Project>`. A project reference doesn't bring them.

```shell
FRAMEWORK_PROJ="../sakura/Sakura.Framework/Sakura.Framework.csproj"
GENERATOR_PROJ="../sakura/Sakura.Framework.SourceGenerators/Sakura.Framework.SourceGenerators.csproj"

dotnet remove Sample.Application/Sample.Application.csproj package Sakura.Framework
dotnet add Sample.Application/Sample.Application.csproj reference "$FRAMEWORK_PROJ" "$GENERATOR_PROJ"

# Make the generator an analyzer, not a referenced assembly.
sed -i.bak -E 's|(<ProjectReference Include=".*Sakura\.Framework\.SourceGenerators\.csproj")[^>]*>|\1 ReferenceOutputAssembly="false" OutputItemType="Analyzer" />|' Sample.Application/Sample.Application.csproj
```

- Trap: never commit the swapped csproj. Revert it with `git checkout -- Sample.Application/Sample.Application.csproj`, then bump the NuGet version once Sakura has published the change.
- The SFDI analyzer warnings and code fixes are wired up by `Sakura.Framework.targets` inside the NuGet package, which a project reference doesn't import. Build against the NuGet package before merging.
- Trap: after switching a phone head either way, delete its `obj/Release` and `bin/Release`, or the next Release link fails ("Symbols were found but are not matching the assembly").

## Write the App class — `App`, `Load`, `OnReady`, `CreateImageLoader`, `CreateAudioManager`, `OnExitRequested`

Keywords: App, root, startup, entry, first screen, resources namespace, image loader, audio manager, close window, quit, exit confirmation, unsaved changes, dispose, shutdown

```csharp
public partial class SampleApp : App
{
    private SampleConfigManager config = null!;
    private SampleScreenStack screenStack = null!;

    protected override Assembly ResourceAssembly => typeof(SampleApp).Assembly;
    protected override string ResourceRootNamespace => "Sample.Application.Resources";

    public override void Load()
    {
        base.Load();

        // Cache first: Add() on a loaded parent loads the child immediately.
        config = new SampleConfigManager(Host.Storage);
        Cache(config);

        screenStack = new SampleScreenStack { RelativeSizeAxes = Axes.Both };
        Cache(screenStack);
        Add(screenStack);
    }

    public override void OnReady()
    {
        base.OnReady();
        screenStack.Push(new StartScreen());
    }

    protected override void Dispose(bool isDisposing)
    {
        config?.Flush();
        base.Dispose(isDisposing);
    }
}
```

- `Load()` builds the root tree. Always call `base.Load()` first: it creates the texture manager, fonts, audio and stores, and caches them.
- `OnReady()` runs once the renderer is ready. Push the first screen there.
- `ResourceAssembly` / `ResourceRootNamespace`: where your embedded `Resources/` folder is. The default is `<namespace of the App class>.Resources`, so set it explicitly.
- Every `Create…` hook already has a working default. Don't override one without a reason you can name:
  - `CreateImageLoader()`: the default `RoutingImageLoader` already decodes JPEG, PNG, WebP and TIFF, applies EXIF rotation, and decodes at a smaller size on request. Replace it only for ICC color management or camera RAW files (see `references/sakura-application.md`, "Decode images").
  - `CreateAudioManager()`: the default (`AudioBackends.Create`) uses BASS if `FrameworkSetting.AudioBackend` asks for it and it loads, otherwise SDL, then BASS, then a silent `HeadlessAudioManager`. It goes silent on its own under a headless host. Return `new HeadlessAudioManager()` only for an app that makes no sound and shouldn't open an audio device.
  - `CreateStorage(host, defaultStorage)`: the default is the app data folder named after the host.
- `OnExitRequested()` runs when the user closes the window. Return `true` to keep the app open (to ask about unsaved work), then call `Host.Exit()` once they confirm. A quick second close forces it. It can't keep a phone app open (see "Handle phones").
- `App` puts everything you `Add` inside its own safe-area container, so `App.InternalChildren` starts with that container and the safe-area preview, not your content (see "Keep content clear of notches").
- Keep `App.Load` short: root containers, app-wide services, overlays. Everything else belongs to screens.

## Load bundled files — `Resources/`, `ITextureManager.Get`, `IAudioStore<T>.Get`, `EmbeddedResourceStorage`

Keywords: embedded resource, asset, bundled image, sample image, sound file, font file, video file, Resources folder, GetStream

Files under the shared project's `Resources/` are embedded, and each kind is found in its own subfolder:

| Folder | Load with |
| --- | --- |
| `Resources/Textures/` | `[Resolved] ITextureManager textures` → `textures.Get("logo.png")` |
| `Resources/Samples/` | `[Resolved] IAudioStore<ISample> samples` → `samples.Get("click.wav")` |
| `Resources/Tracks/` | `[Resolved] IAudioStore<ITrack> tracks` → `tracks.Get("theme.mp3")` |
| `Resources/Videos/` | `[Resolved] VideoStore videos` → `videos.GetDecoder("intro.mp4")` |
| `Resources/Fonts/` | `[Resolved] IFontStore fonts` → `fonts.AddFont(...)` (see "Draw shapes, text and icons") |
| `Resources/Shaders/` | `new CustomShaderStore(new EmbeddedResourceStorage(assembly, "Sample.Application.Resources.Shaders"))` (see "Write a custom shader") |

- To read a bundled file as a stream (to edit a bundled example image, say): `new EmbeddedResourceStorage(typeof(SampleApp).Assembly, "Sample.Application.Resources").GetStream("Textures/example.jpeg")`.
- A test app can merge test-only resources with the app's through `AdditionalResourceAssemblies`.
- A game keeps its assets in a separate resource assembly (see `references/sakura-game.md`, "Lay out a game solution").

## Build a custom drawable — `partial`, constructor, `[BackgroundDependencyLoader]`, `LoadComplete`, `Update`, `Dispose`

Keywords: component, control, widget, custom drawable, lifecycle, load order, Container, Children, Child, Add, Remove, AddInternal, Content, Expire, DisposeOnRemoval

Lifecycle, in order:

| Stage | Runs | Put here |
| --- | --- | --- |
| Constructor | once, on whatever thread creates it | build children, set size and anchors; no dependencies yet |
| `[BackgroundDependencyLoader] private void load(...)` | once, while loading | resolved services, resources, `Cache()` for children |
| `LoadComplete()` | once, after the whole subtree has loaded | subscribe to reactives and events, start animations |
| `Update()` | every frame, on the update thread | per-frame logic; keep it cheap |
| `Dispose(bool)` | when removed (by default) or when the parent is disposed | release what you own, then `base.Dispose(isDisposing)` |

```csharp
public partial class ScoreBadge : Container
{
    private readonly Sprite icon;
    private readonly SpriteText label;

    [Resolved]
    private ITextureManager textures { get; set; } = null!;

    public ReactiveInt Score { get; } = new ReactiveInt();

    public ScoreBadge()
    {
        AutoSizeAxes = Axes.Both;
        Masking = true;
        CornerRadius = 6;

        Children = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Color = Color.DimGray
            },
            new FlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FlowDirection.Horizontal,
                Spacing = new Vector2(4, 0),
                Margin = new MarginPadding(4, 8),
                Children = new Drawable[]
                {
                    icon = new Sprite { Size = new Vector2(16) },
                    label = new SpriteText(),
                }
            }
        };
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        icon.Texture = textures.Get("star.png");
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        OwnReactive(Score);
        BindValueChanged(Score, e => label.Text = e.NewValue.ToString(), runOnceImmediately: true);
    }
}
```

- `Children = …` / `Child = …` in the constructor or initializer; `Add`, `AddRange`, `Remove`, `Clear`, `RemoveAll` at runtime.
- `Remove` and `Clear` dispose by default. To move a drawable elsewhere, `Remove(d, dispose: false)` first; a disposed drawable can't be added again.
- `Expire()` removes the drawable once its running animations finish.
- A container that wraps its content (a panel with a header, a scroll view) overrides `Content` and builds its own parts with `AddInternal`. `Add` then goes into `Content`.

## Share services between drawables — `[Resolved]`, `[Cached]`, `Cache()`, `[BackgroundDependencyLoader]`

Keywords: dependency injection, DI, inject, service, resolve, provide, cache, optional dependency, SFDI0001, SFDI0002, SFDI0003, SFDI0004, SFDI0005, SFDI0006, SFDI0007, partial, reflection fallback

Dependencies flow down the tree: a drawable can resolve anything an ancestor cached.

Provide:

- `Cache(instance)` in a `[BackgroundDependencyLoader]` method or in `App.Load()`. `Cache<ISampleColorProvider>(colors)` registers it under an interface.
- `[Cached]` on a field or property, `[Cached(typeof(ISampleColorProvider))]` for an interface. It is cached before any child loads, so it works for children set in the constructor.

Resolve:

- `[Resolved] private ITextureManager textures { get; set; } = null!;` — needs a setter (SFDI0003) and can't be static (SFDI0005).
- Optional: `[Resolved(canBeNull: true)] private SettingsOverlay? settings { get; set; }` — the type must be nullable (SFDI0007).
- Or as loader parameters: `[BackgroundDependencyLoader] private void load(AppHost host, ITextureManager textures)`.

Already cached by `App`, so resolvable anywhere: `AppHost`, `App`, `IWindow`, `Storage` (the app data folder; null under a headless host), `FrameworkConfigManager`, `ITextureManager`, `IImageLoader`, `IFontStore`, `IAudioManager`, `IAudioStore<ITrack>`, `IAudioStore<ISample>`, `VideoStore`. The renderer is not cached: use `host.Renderer`.

- One `[BackgroundDependencyLoader]` per class (SFDI0004). Name it `load` and make it private. A subclass can declare its own.
- The class and every class it's nested in must be `partial` (SFDI0001, SFDI0002). Without it DI uses the slower reflection fallback.
- Trap: `Cache()` inside a `Load()` override after `base.Load()` is too late for children set in the constructor: they have already loaded and failed to resolve it. Use `[Cached]`, or `Cache()` in the loader method.
- Trap: a test scene, or a screen created outside the App, has none of the app's own services. A missing `[Resolved]` throws on load. Test base classes cache them (see "Write tests").
- One shared service for colors, fonts and sizes keeps every control consistent and themeable: controls ask it for `Surface`, `Accent` or `Danger`, never a literal color (see `references/sakura-application.md`, "Theme the whole app from one provider").

## Size and place things — `Anchor`, `Origin`, `RelativeSizeAxes`, `AutoSizeAxes`, `FlowContainer`, `GridContainer`, `Depth`

Keywords: layout, position, align, center, fill parent, stretch, full width, auto size, wrap content, stack, row, column, grid, table, spacing, gap, margin, padding, z-order, draw order, on top, behind, mask, clip, rounded corners, border, fade edges, MaskingFade, window size, DPI, scale UI, CustomOrigin, pivot, rotate around, BypassAutoSizeAxes, ScreenSpaceDrawQuad, Descendants

- `Anchor` is the point on the parent, `Origin` the point on the child placed on it. `Anchor.Centre` with `Origin = Anchor.Centre` centers.
- Trap: the anchor names are British (`Centre`, `TopCentre`, `CentreLeft`, `BottomCentre`), unlike the rest of the API, which is American (`Color`, `Gray`). `Anchor.Center` doesn't exist. Write everything of your own in American English.
- `CustomOrigin = new Vector2(0.5f, 0.9f)` places, scales and rotates around any point, as a fraction of the drawable's own size (not pixels).
- `RelativeSizeAxes = Axes.Both` makes `Size` a fraction of the parent (`1` fills it). `RelativePositionAxes` does the same for `Position`.
- `AutoSizeAxes` makes a container as big as its children. A child with `BypassAutoSizeAxes` doesn't count. On an axis where a container is both auto-sized and relative-sized, relative wins.
- `Margin` is outside, `Padding` inside. `new MarginPadding(all)`, `(vertical, horizontal)` or `(top, right, bottom, left)`.
- `FlowContainer` stacks its children in the order they were added: `Direction = FlowDirection.Vertical`, `Spacing = new Vector2(0, 8)`, `Alignment`.
- `GridContainer` for rows and columns: `Content = new[] { new Drawable[] { left, right } }` with `ColumnDimensions = new[] { new Dimension(GridSizeMode.Absolute, 280), new Dimension() }`. Modes: `Distributed`, `Relative`, `Absolute`, `AutoSize`.
- `ScrollableContainer` for content bigger than its area: `Direction = ScrollDirection.Vertical`, `ScrollTo`, `ScrollToEnd`, `ScrollIntoView(drawable)`.
- `Depth`: higher draws on top. With equal depth, the child added later is on top. This is the opposite of osu!framework.
- `Masking = true` clips children to the container's bounds. Set it together with `CornerRadius`, `BorderThickness` and `BorderColor`, or `EdgeEffect` for shadows. `CircularContainer` is fully round. `MaskingFade = new MarginPadding { Vertical = 24 }` fades the content out towards the edges instead of cutting it, for a scrolling list.
- `ScreenSpaceDrawQuad` gives the true corners of a rotated or sheared drawable.
- Trap: `Depth` doesn't reorder a `FlowContainer`. Insert children in the order you want them laid out.
- Trap: `ChildrenOfType<T>()` only looks at direct children. For a deep search, `drawable.Descendants().OfType<T>()` (and `DescendantsAndSelf()`) walk `InternalChildren`, parents first. They're newer than `2026.1101.0`; until the next release, write a small recursive walk over `InternalChildren`.
- Trap: `Children` goes through `Content`, so the parts a wrapping container built with `AddInternal` are invisible through it. Walk `InternalChildren` to see everything.
- Scaling to the window: `DrawSizePreservingFillContainer` scales the whole UI to a fixed design size (1920×1080 by default, `TargetDrawSize`). That suits games (see `references/sakura-game.md`, "Scale the UI for every screen"). Desktop tools shouldn't use it (see `references/sakura-application.md`, "Scale the UI like a desktop app").

## Keep content clear of notches — `Bleed`, `SafeAreaContainer`, `SafeAreaDefiningContainer`, Ctrl+F6

Keywords: safe area, notch, MacBook notch, camera housing, punch hole, cutout, Dynamic Island, home indicator, system bars, rounded screen corners, fullscreen, insets, mobile, gesture area, full-bleed background, Edges, Bleed, OverrideSafeArea, SafeAreaPreview

Everything added to an `App` is already laid out in the safe area: `App` puts its content in a `SafeAreaDefiningContainer` (since Sakura 2026.1024.0). Nothing is needed for the UI to stay clear of a notch. What needs work is decoration that should still reach the edge of the screen.

```csharp
// A background or a bar's fill reaches under the notch and the system bars.
new Box
{
    RelativeSizeAxes = Axes.Both,
    Color = colors.Background,
    Bleed = Edges.All
};

// A bar that bleeds keeps its buttons safe by holding them in a SafeAreaContainer.
new Container
{
    RelativeSizeAxes = Axes.X,
    Height = 48,
    Bleed = Edges.Top | Edges.Left | Edges.Right,
    Children = new Drawable[]
    {
        new Box { RelativeSizeAxes = Axes.Both, Color = colors.Bar },
        new SafeAreaContainer { Child = buttons },
    }
};
```

- `Bleed` (`Edges`) extends only the edges that lie on the safe area's boundary. An edge inside the safe area stays where it is, so it's safe to set `Edges.All` on a full-screen background and `Edges.Left | Edges.Right` on a separator.
- The children of a drawable that bleeds are laid out in the bigger rectangle. Put UI inside one in a `SafeAreaContainer`, which pads it back inside the safe area. `SafeAreaOverrideEdges` on it lets content run under chosen edges anyway.
- Bleeding needs the drawable unrotated and its ancestors unrotated and unsheared. It can't escape a masking ancestor. A drawable sheared along one axis (a slanted tab) may bleed on its slanted edges.
- Ctrl+F6 cycles a fake cutout on the desktop: off, iPhone, Android punch hole (`SafeAreaPreview`). Check every screen with it before shipping to a phone.
- `OverrideSafeArea` on a nested `SafeAreaDefiningContainer` makes it a simulated screen with those insets: its descendants bleed to its edges and are padded by its insets. Set it back to `null` to follow the outer safe area. Example: `Sakura.Framework.Tests/Visuals/Containers/TestSafeAreaContainer.cs`.
- On a desktop window the insets are usually zero. They matter on mobile and for a fullscreen window on a MacBook with a notch.
- Trap: without `Bleed`, a fullscreen app on a notched MacBook shows the renderer's black beside the notch. In a light theme that's a dark band across the top. Set `Bleed = Edges.All` on the background, the bar fills and any full-screen scrim.
- Trap: in a test, always set `OverrideSafeArea` on the scene's `SafeAreaDefiningContainer` (`new MarginPadding()` for none). Left `null`, it passes the headless window's safe area through. Assert that the insets took before asserting that something reaches the edge, or the test passes for nothing.

## Draw shapes, text and icons — `Box`, `Circle`, `Line`, `Path`, `PlotCanvas`, `RingArc`, `ParticleField`, `SpriteText`, `FontUsage`, `IconSprite`

Keywords: rectangle, background, circle, dot, line, polyline, triangle, curve, ring, arc, progress ring, spinner, particles, many marks, chart, plot, graph, waveform, text, label, font, font size, weight, bold, italic, letter spacing, text shadow, gradient text, Thai, Japanese, emoji, icon, Material Symbols, truncate, ellipsis, wrap, paragraph, color, hex, gradient, Oklab, contrast

- Shapes: `Box`, `Circle`, `Triangle`, `Line` (`StartPoint`, `EndPoint`, `Thickness`), `Path` (`AddVertex`, `Thickness`, `JointStyle`), `BezierCurve`.
- Many marks in one draw, instead of one drawable each: `PlotCanvas` (`Clear()`, then `Dot`, `Ring`, `Line` with optional dashes, `Area`, `Box`, `Triangle`). Thousands of marks cost one draw, which suits markers drawn over an image, scatter plots and charts. Example: `Sakura.Framework.Tests/Visuals/Drawables/TestPlotCanvas.cs`.
- `RingArc` (`Start`, `Progress`, `InnerRadius`, `SmoothEdges`) for progress rings and spinners. `ParticleField` (`Clear()`, then `Add(new Particle(...))` every frame) for particles. `WaveformGraph` draws an audio `Waveform`.
- Trap: `PlotCanvas` and `ParticleField` fill their parent by default. Set `RelativeSizeAxes = Axes.None` for a fixed size.
- Color: `Color = Color.DimGray`, `Alpha = 0.5f`, `ColorExtensions.FromHex("#3A7BD5")`, `color.Darken(0.2f)`, `color.Lighten(0.2f)`. Gradient: `ColorInfo = ColorInfo.GradientVertical(top, bottom)`. `color.BlendPerceptually(other, 0.5f)` mixes in Oklab (`OklabColor`), so the middle doesn't go gray or muddy.
- WCAG contrast: `color.GetContrastRatio(other)` (1 to 21) and `GetRelativeLuminance()`, for a theme provider's own check. They're newer than `2026.1101.0`; until the next release, keep your own WCAG helper.
- Text: `new SpriteText { Text = "Title", Font = FontUsage.Default.With(size: 14, weight: FontWeights.SemiBold) }`. For an ellipsis: `Truncate = true` with `MaxWidth`. For wrapped text and paragraphs use `TextFlowContainer` (`AddText`, `AddParagraph`, `NewLine`). `ScrollingText` scrolls a line that doesn't fit.
- Style text with `LetterSpacing` (pixels, counted in `MaxWidth`), `Shadow = true` (`ShadowColor`, `ShadowOffset`; a sharp copy, so blur a `BufferedContainer` for a soft one) and `ColorAt` (a color per position, replacing `ColorInfo`). Example: `Sakura.Framework.Tests/Visuals/Text/TestSpriteTextStyles.cs`.
- Trap: an animated `ColorAt` needs `text.Invalidate(InvalidationFlags.Color)` every frame, or it keeps the first colors.
- Fonts that ship with the framework: Noto Sans (variable weight 100 to 900, and italic), Noto Sans Thai, JP, KR, SC, TC, Arabic, Hebrew, Devanagari, and Noto Emoji. These scripts work with no setup.
- Custom fonts: `IFontStore.AddFont(storage, fileName, alias)` or `AddFontFamily`. Example: `Sakura.Framework.Tests/Visuals/Text/TestStaticFont.cs`.
- Name font roles once instead of repeating sizes: a `SampleFont` class with `Title`, `Heading`, `Body`, `Caption` and `Value`.
- Icons: `new IconSprite { Icon = IconUsage.ChevronLeft, IconSize = 16 }`, from Material Symbols. `Style` (outlined, rounded, sharp), `Filled` and `IconWeight` change the look.

## Animate — `FadeTo`, `MoveToX`, `ScaleTo`, `FadeToColor`, `Then`, `Loop`, `Easing`

Keywords: animation, transition, tween, transform, fade, fade in, fade out, slide, move, scale, resize, rotate, spin, flash, color change, delay, wait, sequence, loop, easing, spring, cubic bezier, remove after animation

```csharp
panel.MoveToX(hidden ? -280 : 0, 250, Easing.OutQuint);
background.FadeToColor(Color.SteelBlue, 200);
toast.FadeIn(200).Then(3000).FadeOut(300).Expire();
spinner.Spin(1000);
```

- Every transform takes a target, a duration in ms and an optional easing. Duration `0` is instant: use it when applying state during load, so nothing animates on first show (an `updateVisuals(bool instant)` method that passes `0` when `instant`).
- `Then(delay)` and `Wait(delay)` do the same thing: the next transform starts `delay` ms after the running ones finish. `Loop()` repeats the latest transforms.
- `ClearTransforms()` stops running animations; `FinishTransforms()` jumps them to the end.
- `Easing.OutQuint` is my standard for UI motion. `CubicBezierEasing` and `SpringEasing` also work wherever an easing is accepted.
- A container's auto-size can animate: `AutoSizeDuration`, `AutoSizeEasing`.
- Transforms run on the drawable's clock, on the update thread. Starting one from a background thread breaks rule 2.
- Trap: Sakura retargets a transform that's still running instead of starting it over. Call `ClearTransforms()` before applying the same transition again.

## Handle mouse, keyboard and dropped files — `OnClick`, `OnHover`, `OnDragStart`, `OnKeyDown`, `OnDragDropFile`

Keywords: input, mouse, click, double click, right click, mouse down, hover, drag, pan, scroll, wheel, trackpad, keyboard, key press, key repeat, shortcut, hotkey, modifier, Ctrl, Cmd, drop file, drag and drop, focus, tab order, block input, click-through, ReceivePositionalInputAt, HandlePositionalInput, key binding, rebind, gamepad, controller, OnGamepadButtonDown

Override the handler on the drawable that should react. Return `true` when you handled the event: it stops there. Return `false` to let it carry on to what's behind.

| Handler | Event data |
| --- | --- |
| `OnClick`, `OnDoubleClick`, `OnMouseDown`, `OnMouseUp` | `MouseButtonEvent`: `Button`, `ScreenSpaceMousePosition` |
| `OnHover`, `OnHoverLost`, `OnMouseMove` | `MouseEvent`: `ScreenSpaceMousePosition`, `Delta` |
| `OnDragStart`, `OnDrag`, `OnDragEnd` | Return `true` from `OnDragStart` to capture the drag. Every move then goes to `OnDrag` until release, even outside the drawable. |
| `OnScroll` | `ScrollEvent`: `ScrollDelta`, `AbsoluteDelta` (macOS trackpads scroll one to one) |
| `OnGamepadButtonDown`, `OnGamepadAxisMotion`, `OnGamepadConnected` | `GamepadButtonEvent`, `GamepadAxisEvent` |
| `OnTouchDown`, `OnTouchMove`, `OnTouchUp` | `TouchEvent` (see "Handle phones") |
| `OnKeyDown`, `OnKeyUp` | `KeyEvent`: `Key`, `Modifiers`, `IsRepeat`, `ControlPressed` |
| `OnDragDropFile` | `DragDropFileEvent`: `FilePath`, `Position` |

```csharp
public override bool OnDragDropFile(DragDropFileEvent e)
{
    if (!isSupportedImage(e.FilePath))
        return false;

    openImage(e.FilePath);
    return true;
}
```

- Convert a pointer position into a drawable's coordinates with `ToLocalSpace(e.ScreenSpaceMousePosition)`.
- Keyboard events go to the focused drawable first, then to the rest. Put a screen-wide shortcut in the screen's `OnKeyDown` and an app-wide one in the App's. Check `!e.IsRepeat` for one-shot actions. A key repeat only goes to the drawable that took the first press.
- Trap: `KeyModifiers` only has `Control`, `Shift` and `Alt`. Cmd on macOS isn't reported as a modifier, so shortcuts are Ctrl+… on every platform.
- Trap: the framework takes Ctrl+F1, F2, F3, F6, F7, F9, F10, F11, F12, Ctrl+Shift+L and plain F11 (see "Debug a running app"), before the focused drawable sees them. macOS also keeps Ctrl+F3 to Ctrl+F8 for itself unless its keyboard shortcuts are turned off.
- Trap: returning `true` from `OnHover` un-hovers the parent, and `TooltipContainer` only searches hovered drawables, so every tooltip inside goes dead. Controls inside a tooltip area return `false` from `OnHover`.
- To make an area ignore the pointer without blocking hover, override `ReceivePositionalInputAt(Vector2 screenSpacePos)` (for example, a canvas partly covered by floating panels). `HandlePositionalInput` / `HandleNonPositionalInput` switch input off for a whole subtree.
- Focus and Tab: implement `ITabStop` (`CanBeTabbedTo`, `TabOrder`) and override `AcceptsFocus`. The built-in controls already do. A dialog that keeps Tab inside itself implements `ITabStopScope`, as `FocusedOverlayContainer` does.
- User-rebindable shortcuts: a `KeyBindingContainer<TAction>` with `DefaultKeyBindings`, handlers implementing `IKeyBindingHandler<TAction>` (`OnPressed`, `OnReleased`), saved by `KeyBindingStore<TAction>`, edited in a `KeyBindingPanel<TAction>`. Example: `Sakura.Framework.Tests/Visuals/Input/TestKeyBindingContainer.cs`. A simple tool can handle `OnKeyDown` directly; a game keeps one app-wide container for global keys and one per screen for that screen's actions.
- Gamepads raise their own events. Use them for things like switching on-screen hints to gamepad buttons and waking from idle (gameplay on a gamepad: see "Not covered yet").

## Show tooltips and change the cursor — `IHasTooltip`, `TooltipContainer`, `IHasCursor`, `CursorFeedbackContainer`

Keywords: tooltip, hint, help text, hover text, cursor, mouse pointer, hand cursor, text cursor, grab, grabbing, busy cursor, wait cursor, not allowed

- Any drawable can implement `IHasTooltip` (`string? TooltipText`) and `IHasCursor` (`CursorState Cursor`).
- `CursorState`: `Default`, `Pointer`, `Text`, `Wait`, `Crosshair`, `NotAllowed`, `Grab`, `Grabbing`. Compute it from state, e.g. `public CursorState Cursor => Enabled.Value ? CursorState.Pointer : CursorState.Default;`.
- Add exactly one `TooltipContainer` and one `CursorFeedbackContainer` near the root, last in `App.Load`, so tooltips draw on top of everything.
- Trap: two `CursorFeedbackContainer`s fight over the OS cursor every frame. A `TooltipContainer` only covers its parent's descendants, so one added inside a screen misses everything else.
- For your own tooltip look, subclass `TooltipContainer` and override `CreateTooltip()`.

## Use the built-in controls — `Button`, `Checkbox`, `SliderBar<T>`, `TextBox`, `BasicDropdown<T>`, `ColorPicker`

Keywords: button, checkbox, toggle, switch, slider, number input, text box, text field, input field, dropdown, select, combo box, color picker, key binding panel, themed control, disabled, enabled, Current, Action

- The abstract bases carry the behavior (click, keyboard activation, focus, Tab, drag): `Button`, `Checkbox`, `SliderBar<T>`, `TextBox`, `ColorPicker`, `KeyBindingPanel<T>`. The `Basic*` versions add a placeholder look: `BasicButton`, `BasicCheckbox`, `BasicSliderBar<T>`, `BasicTextBox`, `BasicDropdown<T>`, `BasicColorPicker`, `BasicKeyBindingPanel`.
- For a product, subclass the abstract base and draw your own look (`SampleButton : Button`, `SampleCheckbox`, `SampleSlider`, `SampleTextBox`). Override the hooks: `OnHovered()`, `OnHoverLost()`, `OnEnabledChanged(bool)`, `OnFocusGained()`, `OnFocusLost()`, `OnCheckChanged(bool)`, `OnValueChanged(T)`.
- Values are reactives you can bind:
  - `Checkbox.Current`: `ReactiveBool`
  - `SliderBar<T>.Current`: `ReactiveNumber<T>`, with `MinValue`, `MaxValue`, `Step`, `KeyboardStep`, `DecimalPlaces`
  - `TextBox.Text`: `Reactive<string>`, plus `OnCommit`, `PlaceholderText`, `LengthLimit`, `SelectAll()`, and `InputType` for the phone keyboard
  - `BasicDropdown<T>.Current` with `Items`
  - `ColorPicker.Current`: `Reactive<Color>`
- `Button.Action = () => …` sets what a click does.
- Trap: a button with no `Action` is disabled (`Enabled.Value` is false), and disabled controls are skipped by Tab.

## Show menus and right-click menus — `Menu`, `BasicMenu`, `MenuItem`, `IHasContextMenu`, `ContextMenuContainer`

Keywords: menu, menu bar, dropdown menu, submenu, context menu, right-click menu, popup menu, menu item, separator, toggle item, check item, shortcut text, File menu

```csharp
// Near the root, around everything that offers a right-click menu.
Add(new ContextMenuContainer
{
    RelativeSizeAxes = Axes.Both,
    Child = screenStack
});

// A menu bar.
Add(new BasicMenu(FlowDirection.Horizontal, topLevelMenu: true)
{
    RelativeSizeAxes = Axes.X,
    Items = new MenuItem[]
    {
        new MenuItem("File", new MenuItem[]
        {
            new MenuItem("Save", save) { ShortcutText = "Ctrl+S" },
            new MenuSeparator(),
            new ToggleMenuItem("Show grid", initialState: true),
        })
    }
});

// Any drawable can offer a right-click menu.
public partial class ImageCanvas : Container, IHasContextMenu
{
    public IReadOnlyList<MenuItem>? ContextMenuItems => new MenuItem[]
    {
        new MenuItem("Copy", copy),
        new MenuItem("Paste", paste) { Enabled = { Value = canPaste } },
    };
}
```

- `MenuItem(text, action)` or `MenuItem(text, subItems)`. `Enabled`, `ShortcutText`, `ToggleMenuItem.State` (`ReactiveBool`) are reactives you can bind.
- The framework does the highlight, Up and Down (wrapping, skipping disabled items), Enter, Escape and closing when focus leaves.
- `BasicMenu` is the placeholder look. For a product, subclass `Menu` and `MenuItem`, and override `CreateDrawableMenuItem`, `CreateSubMenu` and the open and close animations. Override `ContextMenuContainer.CreateMenu()` to style right-click menus.
- `ContextMenuItems` is read at the moment of the right-click, so it can depend on state. Return `null` to leave the right-click to whatever is behind, or an empty list to show nothing and stop there.
- Trap: `ShortcutText` is only a label. Handle the key yourself.
- Trap: submenus draw outside their parent's bounds, so nothing between a menu and the screen may have `Masking`.
- Trap: `ContextMenuContainer` doesn't size itself. Set `RelativeSizeAxes`.
- Trap: don't fade a menu in or out. A drawable at `Alpha` 0 can't be clicked or reached with Tab, so a menu that fades in is unusable for its whole entrance, and one that fades out keeps swallowing clicks after it's dismissed.

## React to value changes — `Reactive<T>`, `BindValueChanged`, `BindTo`, `BindBothWaysTo`, `OwnReactive`

Keywords: reactive, bindable, observable, value changed, event, subscribe, unsubscribe, two-way binding, bind control to setting, memory leak, ReactiveInt, ReactiveBool, ReactiveNumber, ValueChangedEvent

- `Reactive<T>` holds a value and raises `ValueChanged` with a `ValueChangedEvent<T>` (`OldValue`, `NewValue`). Also `Default`, `IsDefault`, and `Disabled` to lock the value.
- Numbers: `ReactiveInt`, `ReactiveNumber<T>` (`MinValue`, `MaxValue`, `Precision`; values are clamped on set). Booleans: `ReactiveBool`.
- In a drawable, subscribe with the drawable's own method, in `LoadComplete`:

```csharp
protected override void LoadComplete()
{
    base.LoadComplete();
    BindValueChanged(config.Get<SampleTheme>(SampleSetting.Theme), e => applyTheme(e.NewValue), runOnceImmediately: true);
}
```

- It unsubscribes automatically when the drawable is disposed.
- Trap: `setting.ValueChanged += …` from a drawable keeps the drawable and its whole subtree alive for as long as the app runs, because a config reactive lives that long. It's only safe between objects that live equally long (the App and its own config).
- `a.BindTo(b)` keeps `a` in sync with `b`. `target.BindBothWaysTo(source)` (namespace `Sakura.Framework.Extensions.ReactiveExtensions`) syncs both ways and takes the source's value first: use it for control ↔ setting.
- A control that exposes a reactive for others to bind (its `Current`) calls `OwnReactive(Current)`, so those bindings are cut when it's disposed.
- Trap: `OwnReactive` unbinds every listener of that reactive. Never use it on a shared reactive. That's why a live theme switch walks the tree instead of subscribing every control (see `references/sakura-application.md`, "Theme the whole app from one provider").
- Trap: applying the same state twice (a screen re-applied, a control rebuilt) stacks a second set of `ValueChanged` handlers. Unbind everything from the previous apply first.
- Trap: `ReactiveFloat`'s constructor takes an `int`, so `new ReactiveFloat(0.5f)` doesn't compile. Use `new ReactiveNumber<float>(0.5f)`.

## Save user settings — `ConfigManager<TEnum>`, `[SettingSource]`, `FrameworkConfigManager`

Keywords: settings, preferences, options, config, ini file, persist, save, load, restore, theme setting, window size, renderer, frame limiter, volume, InMemoryConfigManager, Flush

```csharp
[SettingSource("sample.ini")]
public enum SampleSetting
{
    Theme,
    ShowGrid,
}

public class SampleConfigManager : ConfigManager<SampleSetting>
{
    public SampleConfigManager(Storage? storage)
        : base(storage)
    {
        // Register every default first, then read the file over them.
        Get(SampleSetting.Theme, SampleTheme.Dark);
        Get(SampleSetting.ShowGrid, true);
        Load();
    }
}
```

- Create it in `App.Load` with `Host.Storage`, then `Cache(config)`.
- `config.Get<SampleTheme>(SampleSetting.Theme)` returns a `Reactive<T>`. Setting `.Value` saves automatically, debounced by 200ms.
- Give every setting a doc comment saying what it does, and keep `MIN_`/`MAX_` constants for ranged values in the config manager.
- Trap: a change in the last 200ms before quitting is lost unless you call `config.Flush()` on shutdown. Override `Dispose` in your App (see "Write the App class"); on desktop the framework only flushes its own `framework.ini`.
- A `null` storage keeps everything in memory. Tests use that so they never read or overwrite the developer's real settings. `InMemoryConfigManager<T>` does the same.
- The framework's own settings (renderer, frame limiter, window mode and size, volumes, audio device) are in `FrameworkConfigManager` under `FrameworkSetting`. Resolve it to show them in your settings screen.
- Use an enum value for anything with fixed choices (`SampleTheme.Dark`), not a string.

## Move between screens — `ScreenStack`, `Screen`, `Push`, `Exit`, `OnEntering`, `OnExiting`

Keywords: screen, page, view, navigation, navigate, back, go back, push, pop, stack, transition, start screen, workspace

- One `ScreenStack` near the root, cached so screens can push. Give the app its own subclasses (`SampleScreen : Screen`, `SampleScreenStack : ScreenStack`) so shared screen behavior has a home later.
- `screenStack.Push(new ModeSelectScreen())` opens a screen. A screen leaves with `Exit()`.
- A screen fills its parent by default (centered, `RelativeSizeAxes = Axes.Both`).
- Lifecycle: `OnEntering(last)`, `OnSuspending(next)`, `OnResuming(last)`, `OnExiting(next)`. Put transitions in them. The stack waits for the longest transform before removing an exiting screen, and hides a suspended screen that defined no transition.
- Only the current screen receives mouse input.
- Escape goes back, as on every desktop app.
- Push the first screen from `App.OnReady`.
- A screen can declare what it needs from the shared parts of the app (footer, background, music, cursor) as virtual properties that the stack applies. See `references/sakura-game.md`, "Make screens declare what they need".

## Show panels, popups and dialogs — `VisibilityContainer`, `OverlayContainer`, `FocusedOverlayContainer`

Keywords: overlay, popup, modal, dialog, confirm dialog, settings panel, drawer, sheet, show, hide, toggle, escape to close, PopIn, PopOut, block input behind

```csharp
public partial class ConfirmDialog : FocusedOverlayContainer
{
    protected override void PopIn() => this.FadeIn(200, Easing.OutQuint);

    protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);
}
```

- `VisibilityContainer`: implement `PopIn()` and `PopOut()` with transforms, then call `Show()`, `Hide()` or `ToggleVisibility()` and read `State`. It starts hidden (`StartHidden`).
- `OverlayContainer` also blocks mouse input to whatever is behind it while visible (`BlockPositionalInput`).
- `FocusedOverlayContainer` also takes focus, keeps Tab inside, blocks keyboard input to the rest of the app, and closes on Escape (`CloseOnEscape`). Use it for modal dialogs and a settings overlay.
- Add overlays to the App root above the screen stack, and cache any a screen needs to open.

## Run slow work without freezing the UI — `Task.Run`, `Scheduler.Add`, `Scheduler.AddDelayed`, `LoadComponentAsync`

Keywords: thread, threading, background, async, await, freeze, hang, not responding, responsive, worker, debounce, throttle, cancel, stale result, generation, update thread, draw thread, ExecutionMode, LongRunningLoad, ScheduledTask

Sakura runs an update thread (owns the drawable tree), a draw thread (owns the GPU) and an audio thread, while the main thread runs the window and collects input. Ctrl+F7 switches between multi-threaded and single-threaded execution while debugging.

The pattern for heavy work:

```csharp
private int previewGeneration;

private void runPreview()
{
    // 1. On the update thread: copy what the work needs out of the drawables.
    double strength = strengthSlider.Current.Value;
    int generation = ++previewGeneration;
    var scheduler = Scheduler;

    // 2. In the background: touch no drawable.
    Task.Run(() =>
    {
        try
        {
            PreviewResult result = buildPreview(strength);
            scheduler.Add(() => commitPreview(result, generation));
        }
        catch (Exception ex)
        {
            scheduler.Add(() => Logger.Error("Preview failed", ex));
        }
    });
}

// 3. Back on the update thread.
private void commitPreview(PreviewResult result, int generation)
{
    if (generation != previewGeneration)
    {
        // A newer request replaced this one.
        result.Dispose();
        return;
    }

    preview.Show(result);
}
```

- `Scheduler.Add` is thread-safe and runs the action on the drawable's update thread in the next frame. If the drawable is disposed first, its scheduler is cleared and the action never runs.
- Trap: `Scheduler` itself is created on first use, and that creation isn't thread-safe. Read it on the update thread into a local before `Task.Run` (as above), then call the local from the background.
- Debounce work driven by a slider:

```csharp
private ScheduledTask? pendingPreview;

private void onSliderMoved()
{
    Scheduler.Cancel(pendingPreview);
    pendingPreview = Scheduler.AddDelayed(runPreview, 150);
}
```

- Run one job at a time and show that something is happening (see `references/sakura-application.md`, "Run one job at a time and show it").
- `Scheduler.AddRepeating(action, delay, interval)` for polling.
- Heavy drawables: `LoadComponentAsync(drawable, d => container.Add(d))` runs its loading on a background thread and adds it on the update thread. Mark a type `[LongRunningLoad]` to force that; adding it synchronously then throws.
- Trap: `Add()` on a parent that has already loaded loads the child right there, synchronously. A child with a slow loader freezes the frame. Called from a background thread, it loads off-thread and changes the tree from the wrong thread.

## Show pixels on screen — `Sprite`, `Texture`, `ITextureManager.FromPixelData`, `ScheduleToDrawThread`

Keywords: image, picture, photo, display image, texture, sprite, pixels, bitmap, RGBA, upload, GPU, preview, swap image, replace texture, free texture, VRAM, texture leak, IsAvailable, TextureFillMode, ImageLoadOptions

- A bundled image: `sprite.Texture = textures.Get("logo.png")` (from `Resources/Textures`). Decode it smaller with `textures.Get("photo.jpeg", ImageLoadOptions.MaxDimension(512))` or `ImageLoadOptions.FillTarget(size)`.
- `sprite.FillMode`: `TextureFillMode.Stretch`, `Fit`, `Fill` or `Tile`.
- Pixels you made yourself (8-bit RGBA, row by row) become a texture on the draw thread:

```csharp
[Resolved]
private AppHost host { get; set; } = null!;

[Resolved]
private ITextureManager textures { get; set; } = null!;

private void upload(ImageRawData pixels, string name)
{
    host.Renderer.ScheduleToDrawThread(() =>
    {
        Texture texture;

        try
        {
            texture = textures.FromPixelData(pixels.Width, pixels.Height, pixels.Data);
            texture.Name = name;
        }
        finally
        {
            // FromPixelData has copied the pixels.
            pixels.Dispose();
        }

        Scheduler.Add(() => showWhenUploaded(texture));
    });
}
```

- The GPU upload itself is spread over the following frames within a per-frame byte budget.
- Trap: showing a texture before `texture.IsAvailable` is true draws garbage. Keep the old texture on screen and swap once the new one is available (check `IsAvailable` in `Update`).
- To free a replaced texture, wait a little and dispose it on the draw thread. A draw node from a frame or two ago, and Metal's own frame pipeline, may still be drawing it:

```csharp
Scheduler.AddDelayed(() => host.Renderer.ScheduleToDrawThread(oldTexture.Dispose), 150);
```

- Trap: call `Texture.Dispose()`, never `BackendTexture.Dispose()`. The second frees the GPU memory but leaves the texture registered, so the texture viewer and every memory counter report it as alive forever.
- Pass no `cacheKey` to `FromPixelData` for a texture you replace often. With a key, the manager releases the previous texture on the next frame, which is too early for the delayed swap above.
- Set `texture.Name`. The texture viewer (Ctrl+F3) is useless with anonymous textures.
- A texture shown in many places at once (thumbnails, cover art): `AcquireSharedTexture(cacheKey, …)` and `ReleaseSharedTexture(cacheKey)`.
- Decoding and processing big images (color management, RAW, memory): `references/sakura-application.md`.

## Blur, grayscale and cached rendering — `BufferedContainer`, `EdgeEffect`

Keywords: blur, frosted glass, grayscale, effect, glow, shadow, drop shadow, framebuffer, render to texture, cache drawing, offscreen

- `BufferedContainer` draws its children into an offscreen buffer first: `BlurSigma = new Vector2(8)`, `GrayscaleStrength`, `EffectColor`, `DrawOriginal`. `CacheDrawnFrameBuffer = true` reuses the result until `ForceRedraw()`, which helps with complex content that rarely changes.
- Shadows and glow on a container: `EdgeEffect` with `Masking = true`.
- Buffers cost GPU memory (`NativeMemoryCategory.FrameBuffers`). Don't wrap something that changes every frame.

## Write a custom shader — `CustomShaderDrawNode<TBlock>`, `CustomShaderStore`, `ShaderChecks`, GLSL 450

Keywords: shader, GLSL, fragment shader, vertex shader, custom rendering, DrawNode, CreateDrawNode, ApplyState, GenerateVertices, Topology, uniform, uniform block, std140, SPIR-V, Metal, HLSL, Direct3D 11, Vulkan, OpenGL, cross-compile, color filter, LUT, glow, light, sh_Custom.glsl, writeFragment, ICustomShaderRenderer

First check you need one (rule 9):

- blur, grayscale and glow: `BufferedContainer`
- tints, gradients, transparency, additive or multiply looks: `Color`, `ColorInfo`, `Alpha`, `Blending` (`Alpha`, `Additive`, `Opaque`, `Multiply`, `Screen`, `Premultiplied`)
- many dots, lines or particles: `PlotCanvas`, `ParticleField` (see "Draw shapes, text and icons")
- per-pixel image processing that runs once per edit: do it on the CPU with Magick or OpenCV and upload the result with `FromPixelData`

A shader is worth it when the per-pixel result has to change every frame, or the CPU path is too slow on a full-size image. A game's screen-edge glow, light rays and result-screen effects are good fits. The API arrived in Sakura `2026.1101.0`.

1. Write GLSL 450 in `Resources/Shaders/`. Sakura cross-compiles it through SPIR-V to Metal, HLSL, Vulkan and OpenGL and caches the result on disk, so one source runs on every renderer. Start from the framework's `sh_Custom.glsl`, which declares the inputs and `FragColor`, and end with `writeFragment`, which keeps the pixel inside any mask (rounded corners, scroll areas, masking fades):

```glsl
#version 450

#include "sh_Custom.glsl"

layout(set = 0, binding = 5, std140) uniform GainBlock
{
    vec4 u_Color;   // linear
    float u_Gain;
};

void main()
{
    writeFragment(vec4(u_Color.rgb * u_Gain, u_Color.a * v_Color.a));
}
```

   - Your uniform blocks go in `set = 0`, bindings 5 to 7 (0 to 4 are the framework's), with `std140`, at most 256 bytes each. Textures are `set = 1`, bindings 0 to 3. Push constants and storage buffers are rejected.
   - Don't redeclare what `sh_Custom.glsl` declares: `v_Color`, `v_TexCoords` and the clip inputs, and `FragColor`.
   - Everything in the shader is linear light: vertex colors arrive linear and the framebuffer converts back to sRGB. Pass colors in linear (`ColorExtensions.SrgbToLinear`), or `pow(c, vec3(2.2))` a color typed into the shader.
   - Shared code for all of an app's shaders goes in its own include that includes `sh_Custom.glsl` (for example `sample.glsl`, with the app's colors and a `writeLight` helper for additive light).

2. Keep one `CustomShaderStore` for the app's shaders. It compiles each shader once per renderer and recompiles if the renderer is replaced:

```csharp
public static class AppShaders
{
    public static readonly Storage STORAGE = new EmbeddedResourceStorage(typeof(SampleApp).Assembly, "Sample.Application.Resources.Shaders");

    public static readonly CustomShaderStore STORE = new CustomShaderStore(STORAGE);
}
```

3. A C# struct matches the block byte for byte, in std140 layout: `float` and `int` take 4 bytes, `vec2` aligns to 8, `vec4` and `mat4` align to 16, a GLSL `bool` is an `int`, and the size is a multiple of 16.

4. The drawable returns a `CustomShaderDrawNode<TBlock>`. The delegate runs on the update thread whenever the draw node copies state, so it may read the drawable:

```csharp
public partial class GainBox : Drawable
{
    private float gain = 1;

    public float Gain
    {
        get => gain;
        set
        {
            gain = value;

            // The draw node only re-reads the block after an invalidation.
            Invalidate(InvalidationFlags.DrawInfo);
        }
    }

    protected override DrawNode CreateDrawNode() => new CustomShaderDrawNode<GainBlock>(AppShaders.STORE, "gain.frag", nameof(GainBlock), block);

    private GainBlock block() => new GainBlock
    {
        Color = new Vector4(ColorExtensions.SrgbToLinear(Color.R), ColorExtensions.SrgbToLinear(Color.G), ColorExtensions.SrgbToLinear(Color.B), DrawAlpha),
        Gain = gain
    };

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct GainBlock
    {
        [FieldOffset(0)]
        public Vector4 Color;

        [FieldOffset(16)]
        public float Gain;
    }
}
```

5. Check every shader builds on every renderer in `dotnet test`. `ShaderChecks` (in `Sakura.Framework.Testing`) compiles for GLSL, HLSL, MSL and Vulkan on any machine:

```csharp
[TestCase("gain.frag")]
public void TestShaderBuilds(string shader) => Assert.That(ShaderChecks.Problems(AppShaders.STORAGE, shader), Is.Empty);
```

- A drawable that isn't a plain quad overrides `Topology` (for example `VertexTopology.Triangles`) and `GenerateVertices`, and can put its own values in each vertex's `TexCoords` for the shader.
- More than one block, or a texture: subclass `CustomShaderDrawNode`, copy what it needs in `ApplyState`, and upload in `SetUniforms(IShader shader)`. For a texture, the framework's own pattern is `shader.SetUniform("u_Texture", 0); texture.BackendTexture?.Bind(0);` with `layout(set = 1, binding = 0) uniform sampler2D u_Texture;`, and a check for `texture.IsAvailable`. No test covers a textured custom shader yet: check it on every renderer.
- Your own vertex shader: pass its path as the last argument. It must output what `shader.vert` outputs, at locations 0 to 8.
- Test on every renderer the app ships with. Switch with `FrameworkSetting.RendererType` (`Metal`, `Direct3D11`, `Vulkan`, `OpenGL`) in `framework.ini`. Vulkan is first on Linux and the fallback after Direct3D 11 on Windows, and the only renderer on Android.
- Trap: a shader that doesn't build is logged once and then simply not drawn, and only on the renderer where it fails. That's what `ShaderChecks` is for.
- Trap: a block size that isn't a multiple of 16 throws `ArgumentException` when the draw node is created. Pad the struct.
- Trap: a custom shader draws nothing headless, or where it doesn't build. Anything that must always show (a judgement, a warning) needs a layer of its own that doesn't use one. Pixel assertions need `[VisualTestOnly]`.
- Trap: older custom-shader code that switches on `IGLRenderer`, `IMetalRenderer` and `ID3D11Renderer` to call each renderer's raw draw draws nothing on Vulkan. Use `CustomShaderDrawNode`, which goes through `ICustomShaderRenderer` on every renderer.

## Show many items efficiently — `DrawablePool<T>`, `PoolableDrawable`, `DelayedLoadWrapperContainer`

Keywords: list, long list, many items, thousands, performance, recycle, reuse, pool, virtualize, lazy load, off-screen, thumbnails

- `DrawablePool<T>`: add the pool to the tree, then `pool.Get(item => item.Setup(data))`. Items derive from `PoolableDrawable` and go back to the pool when removed (or with `Return()`).
- A pooled item comes back as it was left. Its `Setup` resets everything the last use changed: `ClearTransforms()`, alpha, scale, rotation, children.
- Pool anything created per frame or per event (list rows, hit effects, markers), so the GC has nothing to collect mid-animation.
- `new DelayedLoadWrapperContainer(() => new Thumbnail(path), timeBeforeLoad: 500)` loads its content only after it has been on screen for a while. `DelayedLoadUnloadWrapperContainer` also unloads it after it leaves the screen.

## Open and save files — `ShowOpenFileDialog`, `ShowSaveFileDialog`, `FileDialogOptions`, `Storage`

Keywords: file picker, open file, save as, choose folder, export, import, file dialog, filter, extension, app data folder, storage, logs folder, open in Finder, reveal in Explorer, open URL, open link, OpenFileExternally

```csharp
private static readonly FileDialogFilter[] image_filters =
{
    new FileDialogFilter("Images", "jpg", "jpeg", "png", "tif", "tiff"),
    FileDialogFilter.AllFiles(),
};

private void pickImage()
{
    host.ShowOpenFileDialog(new FileDialogOptions
    {
        Title = "Open an image",
        Filters = image_filters
    }, result =>
    {
        if (result.Successful && result.Path.IsNotNull())
            screenStack.Push(new EditorScreen(result.Path));
    });
}
```

- The callback runs on the update thread, so it can touch drawables. Inside the App use its protected `ShowOpenFileDialog`; elsewhere resolve `AppHost`.
- `ShowSaveFileDialog` and `ShowOpenFolderDialog` work the same. `AllowMultiple = true` fills `result.Paths`. `result.Error` explains a failure.
- Extensions in a filter have no leading dot.
- The app's own files (settings, logs, caches, exports it keeps): resolve `Storage`.
  - `GetStream(path, FileAccess.Write, FileMode.Create)` to write, `GetStream(path)` to read
  - `CreateFileSafely(path)` to replace a file without a half-written result if the app crashes
  - `GetStorageForDirectory("exports")`, `GetFullPath(path)`, `Exists(path)`, `GetFiles(path)`
  - Logs go to `logs/` inside it.
- Hand off to the operating system: `host.OpenFileExternally(path)`, `host.PresentFileExternally(path)` (reveal in Finder or Explorer), `host.OpenUrlExternally(url)`.
- Trap: `Storage` is `null` under a headless host, which tests use. Accept `Storage?` wherever you take one (the config manager above does).

## Play sounds and video — `IAudioStore<ISample>`, `IAudioStore<ITrack>`, `VideoSprite`

Keywords: decoder, built-in decoder, sound, audio, sample, sound effect, beep, music, track, volume, mute, output device, speakers, headphones, video, movie, playback, FFmpeg, HeadlessAudioManager, latency, audio buffer, AudioDeviceBufferFrames, underrun, crackle, zero-copy, VideoZeroCopy, hardware decoding, VideoToolbox, HDR

- Audio works with no setup: the framework picks SDL audio, or BASS if `FrameworkSetting.AudioBackend` asks for it, and falls back to silence if neither loads. Only an app that makes no sound overrides `CreateAudioManager()` to return `new HeadlessAudioManager()`.
- Short sounds: `samples.Get("click.wav").Play()`.
- Music: `IAudioChannel channel = tracks.Get("theme.mp3").GetChannel();`, then `channel.Play()`, `Pause()`, `Stop()`. Dispose the channel when finished.
- Volume settings: `FrameworkSetting.MasterVolume`, `TrackVolume` and `SampleVolume` in `FrameworkConfigManager`.
- Output device: `IAudioManager.OutputDevices`, `OutputDevicesChanged`, and `OutputDevice` (a `Reactive<string>`; empty means the system default), saved as `FrameworkSetting.AudioDevice`. `AudioBackends.GetSuitableBackends()` and `RendererTypes.GetSuitableRenderers()` list the choices for a settings dropdown.
- Music that drives gameplay (clocks, offsets, tempo, latency): `references/sakura-game.md`, "Keep music and gameplay in time".
- Video: `new VideoSprite(filePath)` (or from a stream, or `videos.GetDecoder(name)`), then `Play()`, `Pause()`, `Stop()`, `Seek(ms)`. Decoding is built in: `VideoDecoder` runs FFmpeg, which ships in `Sakura.Framework.NativeLibraries`, with hardware decoding on by default (`FrameworkSetting.HardwareAcceleration`, `FrameworkSetting.VideoZeroCopy`). Don't add another decoder.
- Zero-copy video (`FrameworkSetting.VideoZeroCopy`, on by default) samples the hardware decoder's frame on the GPU instead of reading it back and uploading it. Whether a frame qualifies is decided per frame by the renderer:
  - Metal (macOS, iOS): VideoToolbox frames that are 8-bit 4:2:0 (NV12, the usual H.264 and HEVC). 10-bit HDR takes the copy path.
  - Direct3D 11: 8-bit D3D11 frames; 10-bit (P010) takes the copy path.
  - Vulkan (Linux, Android, Windows fallback): NV12 frames decoded on the renderer's own device.
  - OpenGL (the fallback everywhere, macOS included) and headless: never.
- A frame that can't go zero-copy takes the copy path without any error, so leave the setting on. It needs hardware decoding (`FrameworkSetting.HardwareAcceleration`). To see which path a video takes: Ctrl+F2 → Video → "Frame Format" ends in "(zero-copy)" when it's active.
- Audio output latency is `FrameworkSetting.AudioDeviceBufferFrames`, the device buffer asked of SDL. The default depends on the platform: 128 frames on the desktop (about 2.7 ms at 48 kHz), 256 on iOS, and 0 on Android, which means "the device's own burst size" and keeps the stream on the low-latency path. Values are clamped to 32–8192.
- It applies to the native SDL engine only. The managed mixer (`AudioBackend.SDLManaged`) keeps its own 80 ms queue and ignores it.
- When the output underruns, Sakura doubles `AudioDeviceBufferFrames` for the next launch and logs a warning. Set it back in `framework.ini` to try the smaller buffer again.

## Log and handle errors — `Logger`, `LoggingTarget`, `AppHost.ExceptionThrown`

Keywords: log, logging, debug output, console, error, exception, crash, stack trace, log file, log level, unhandled exception, Serilog

- In a Sakura app use Sakura's static `Logger`, not Serilog: `Logger.Verbose`, `Logger.Debug`, `Logger.Warning`, `Logger.Error`, `Logger.Fatal`, each with an optional `LoggingTarget` (`Runtime`, `Performance`, `Network`, `Graphics`, `Database`).
- Exceptions: `Logger.Error("Saving the result failed", ex)` logs the message plus the full exception and stack trace. Keep context in the message: `Logger.Error($"Failed to load image from {path}", ex)`.
- Trap: `Logger.Error($"Saving failed: {ex.Message}")` throws the stack trace away.
- Trap: `catch (Exception) { }` that skips a bad file without logging makes it vanish with no trace. Skip it if you must, but log it.
- Logs go to the console and to `logs/` in the app data folder.
- `host.ExceptionThrown += ex => …` sees unhandled exceptions. Returning `true` keeps the app running; only do that for errors you know are safe to ignore.

## Debug a running app — draw visualizer, statistics, texture viewer

Keywords: debug, debugging, inspect, inspector, draw tree, hierarchy, FPS, frame time, display latency, frame limiter, performance, slow, memory, VRAM, texture leak, statistics, profiler, overlay, log overlay, safe area preview, storage folder, renderer crash, GlobalStatistics, Name

| Keys | Tool |
| --- | --- |
| Ctrl+F1 | Draw visualizer: the drawable tree. Ctrl+Shift+F1 picks a drawable on screen; the scroll wheel steps back through drawables stacked under the pointer. |
| Ctrl+F2 | Global statistics: memory, native memory per category, textures, GC |
| Ctrl+F3 | Texture viewer: every live texture, by `Name` |
| Ctrl+F6 | Safe-area preview: off, iPhone, Android punch hole |
| Ctrl+F7 | Switch between multi-threaded and single-threaded execution |
| Ctrl+F9 | Audio mixer |
| Ctrl+F10 | Cycle the frame limiter |
| Ctrl+F11 | FPS graph: hidden, minimal, compact, expanded. Shows display latency, the renderer and the audio backend. |
| Ctrl+F12 | Open the app data folder |
| Ctrl+Shift+L | Log overlay |
| F11 | Window mode |

- On a phone, a four-finger tap opens them in a Debug build.
- A renderer that crashed during startup is moved to the back of the order and recorded in `renderer-crashes.txt` beside `framework.ini`. It's tried after the others until it starts, or until it's chosen in the settings again.
- `DrawHierarchyUtils.PrintAppHierarchy(app)` logs the whole tree, for when the visualizer can't be opened.
- Set `Name` on important drawables and textures so you can find them in these tools.
- Publish your own numbers: `GlobalStatistics.Get<int>("Sample", "Items processed").Value = count;`.
- Show the build somewhere visible: see `references/sakura-application.md`, "Show which build is running".

## Hot reload while you work — `dotnet watch`, `HotReloadManager.OnHotReload`

Keywords: hot reload, live reload, edit and continue, apply changes, dotnet watch, iterate, change code without restart, test browser reload, MetadataUpdateHandler

- Sakura works with .NET Hot Reload. Run `dotnet watch run --project Sample.Application.Tests`, or use hot reload in Rider or Visual Studio while debugging. Edited code applies without restarting.
- The visual test browser is the fastest loop: on every reload it rebuilds the open scene and flashes "Hot Reloaded!", so constructor and layout changes show straight away. Build a control in its test scene first, then put it in the app.
- In the running app nothing is rebuilt. Drawables that already exist keep what their constructor and loader built; only code that runs again (`Update`, input handlers, drawables created after the reload) picks up the change. To rebuild a screen on reload, subscribe in the App:

```csharp
protected override void LoadComplete()
{
    base.LoadComplete();

    // Fires on the thread that applied the edit, so hop back to the update thread first.
    HotReloadManager.OnHotReload += () => Scheduler.Add(reloadCurrentScreen);
}
```

  `HotReloadManager` lives in `Sakura.Framework.Testing`. `reloadCurrentScreen` is yours to write, for example exit the current screen and push a new instance of it. The event never fires in a release build.
- DI keeps up: Sakura clears its dependency-injection caches on every reload, so new or renamed `[Resolved]` members resolve in drawables created afterwards.
- Some edits can't be applied to a running process, for example changing a class's base type. `dotnet watch` then offers to restart.

## Write tests — `TestScene`, `ManualInputManagerTestScene`, `TestBrowserApp`, `[VisualTestOnly]`

Keywords: test, testing, unit test, visual test, NUnit, headless, test browser, steps, AddStep, AddAssert, AddUntilStep, simulate click, simulate keyboard, InputManager, MoveMouseTo, CI, dotnet test

The test project (`Sample.Application.Tests` or `Sample.Game.Tests`) is a runnable app as well as an NUnit project:

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <GenerateProgramFile>false</GenerateProgramFile>
  <IsPackable>false</IsPackable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
  <PackageReference Include="NUnit" Version="4.6.1" />
  <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
  <PackageReference Include="NUnit.Analyzers" Version="4.14.0" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="..\Sample.Application\Sample.Application.csproj" />
</ItemGroup>
```

```csharp
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using AppHost host = new DesktopAppHost("sample-tests");
        using App app = new SampleTestApp(typeof(Program).Assembly);
        host.Run(app);
    }
}

public class SampleTestApp : TestBrowserApp
{
    public SampleTestApp(Assembly testAssembly)
        : base(testAssembly)
    {
    }

    protected override Assembly ResourceAssembly => typeof(SampleTestApp).Assembly;
    protected override string ResourceRootNamespace => "Sample.Application.Tests.Resources";
}
```

- `dotnet run --project Sample.Application.Tests` opens the visual test browser: pick a scene, watch its steps run, and click through them by hand.
- `dotnet test` runs the same scenes headless as NUnit tests, at 2× clock speed (`TestScene.HeadlessClockRate`).
- Folders: `Visuals/` for scenes, `Unit/` for plain NUnit `[TestFixture]` classes that test logic (maths, parsing, validation) with no drawables.

A scene:

```csharp
public partial class TestSampleButton : SampleControlTestScene
{
    private SampleButton button = null!;
    private int clicks;

    [SetUp]
    public void SetUp()
    {
        clicks = 0;
        CreateContent(() => button = new SampleButton
        {
            Text = "Press me",
            Action = () => clicks++
        });
    }

    [Test]
    public void TestClickFiresAction()
    {
        AddStep("Click the button", () =>
        {
            InputManager.MoveMouseTo(button);
            InputManager.Click(MouseButton.Left);
        });
        AddAssert("Action fired once", () => clicks == 1);
    }
}
```

- Steps: `AddStep`, `AddAssert`, `AddUntilStep` (waits for a condition, 10 s timeout), `AddWaitStep`, `AddRepeatStep`, `AddSliderStep` (a live slider in the browser), `AddLabel`.
- Input, in a `ManualInputManagerTestScene`: `InputManager.MoveMouseTo(drawable)`, `Click`, `DoubleClick`, `Drag`, `ScrollBy`, `PressKey`, `ReleaseKey`, `TypeText`, `ChangeFocus`. Put content in `TestContent`.
- Give each kind of scene a base class that caches what the App normally provides (see `references/sakura-application.md`, "Test controls and screens", and `references/sakura-game.md`, "Test a game").
- `[VisualTestOnly("reason")]` on a test or class skips it in the headless run, for tests that need a real renderer.
- To run the whole app inside a scene: create the App, `SetHost(host)`, then `AddApp(app)`. Use a new App in each test: a shown App is disposed when the scene clears.
- Every bug fix comes with a regression test.
- Trap: headless text has no glyph metrics, so every `SpriteText` measures 0. Never assert on anything derived from text width.
- Trap: NUnit reuses one fixture instance for every test in the class, and `SetupNUnit` calls `Clear()` before each test, which disposes the scene's children, including anything built once in the loader. Tests then run against a disposed, detached tree without throwing, and every size reads zero. Override `Clear` to keep the long-lived parts (with `DisposeOnRemoval = false`), or rebuild them in `[SetUp]`.
- `DrawSize` and auto-size are worked out when read, so they're current right after a change. `DrawRectangle`, `ToScreenSpace` and vertices still use the last frame's layout.
- Trap: positions on screen settle over a frame or more. Wait until a measurement stops changing before asserting on it (a step that waits until two frames agree).

## Add iPhone, iPad and Android — `--ios`, `--android`, `SakuraApplication.Run`, `SakuraActivity`

Keywords: mobile, phone, tablet, iOS, iPadOS, iPhone, iPad, Android, head project, net11.0-ios, net10.0-android, workload, Xcode, JDK, Info.plist, AndroidManifest, entry point, MainActivity, AppDelegate, simulator, emulator, install on device, devicectl, adb, sign, provisioning profile, keystore, aab, apk, Vulkan

The shared project (`.Application` or `.Game`) stays `net10.0` and references `Sakura.Framework`. Each phone head references the shared project plus one head package, pinned to the same version:

```shell
dotnet new sakura -n Sample --ios --android     # or add the heads to an existing app by copying them from the template
```

| Head | Target | Package | Entry point |
| --- | --- | --- | --- |
| `Sample.iOS` | `net11.0-ios`, iOS 15+ | `Sakura.Framework.iOS` | `Program.Main` calls `SakuraApplication.Run` |
| `Sample.Android` | `net10.0-android`, API 29+, `android-arm64` | `Sakura.Framework.Android` | `MainActivity : SakuraActivity` |

```csharp
// iOS. SDL owns UIKit, so Run never returns.
public static void Main(string[] args) => SakuraApplication.Run("sample", () => new SampleApp(), () => new HostOptions { FriendlyAppName = "Sample" });

// Android. Keep the template's [Activity] attribute (SingleInstance, ConfigurationChanges, theme) and change only these.
public class MainActivity : SakuraActivity
{
    protected override App CreateApp() => new SampleApp();
}
```

- Toolchain: the .NET 11 SDK with `dotnet workload install ios` and `dotnet workload install android`, Xcode 27 on a Mac, JDK 21 and the Android SDK. iOS needs .NET 11 because that's where it gets CoreCLR; the desktop and tests stay on .NET 10.
- Use the same data folder name (`"sample"`) on every head.
- Both head csproj files keep `<EmbeddedResource Remove="Resources\**" />`: `Resources/` there is the platform's own folder. App assets belong in the shared project.
- From NuGet, the heads' build fixes come in through the packages' `buildTransitive` files with nothing to add. Against a local checkout the head has to import them itself (see "Work against a local Sakura checkout").
- iOS `Info.plist` from the template: `UISceneDelegateClassName` = `SDLUIKitSceneDelegate` (SDL owns the scene), `CADisableMinimumFrameDurationOnPhone` for 120 Hz, arm64 and metal required. Add `UIFileSharingEnabled` and `LSSupportsOpeningDocumentsInPlace` to show the app's files in Finder and the Files app, and `UTImportedTypeDeclarations` plus `CFBundleDocumentTypes` to open your own file type.
- `AndroidManifest.xml`: Vulkan 1.1 is required, as it's the only Android renderer. A game keeps `android:appCategory="game"` and the game-mode meta-data; a tool drops both and sets `HostOptions.AppType = AppType.Application`. Orientation comes from the activity's `ScreenOrientation` (a landscape game: `SensorLandscape`).
- `HostOptions.AppType` is `Game` by default. On a phone it requests the highest refresh rate, defers the home indicator and edge swipes, keeps the screen on, takes audio focus and uses unbuffered touch. A tool sets `AppType = AppType.Application`.
- `RuntimeInfo.IsMobile` and `RuntimeInfo.IsApple` for platform checks.

Build and run:

```shell
# iOS simulator
dotnet build Sample.iOS -t:Run -p:RuntimeIdentifier=iossimulator-arm64

# iPhone or iPad
dotnet build Sample.iOS -c Release -r ios-arm64
xcrun devicectl list devices
xcrun devicectl device install app --device <UDID> Sample.iOS/bin/Release/net11.0-ios/ios-arm64/Sample.iOS.app
xcrun devicectl device process launch --device <UDID> com.example.sample

# Android phone or emulator (Debug)
dotnet build Sample.Android -t:Install
```

- iOS signing is Xcode's automatic signing. A free Apple account works, but its profiles last 7 days. Wrap the device steps in a script (`install-iphone.sh`).
- Android Release: `dotnet publish -c Release -f net10.0-android -p:AndroidPackageFormats=apk%3Baab` with `-p:AndroidKeyStore=true` and the signing properties, passwords as `env:`. `ApplicationVersion` (the version code) must keep growing: a date-based `YYMMDDPP` does.
- Version an iOS build with `-p:Version=… -p:ApplicationDisplayVersion=… -p:ApplicationVersion=…`. Velopack has no phone support.
- Trap: a Debug Android build keeps its assemblies on the device, not in the APK. Install it with `-t:Install` or `-t:Run`; the APK alone won't start.
- Trap: the Android emulator's API 36 image has a broken Vulkan driver. Use API 35. Warning `XA1040` (CoreCLR on Android is experimental) is expected.
- Trap: a configuration change the activity doesn't list in `ConfigurationChanges` restarts the activity, and SDL then kills the app. Besides the template's, list `Locale`, `LayoutDirection`, `FontScale`, `FontWeightAdjustment`, `GrammaticalGender`, `ColorMode`, `Touchscreen`, `Mcc` and `Mnc`.
- Trap: switching a head between NuGet and a local Sakura leaves a linker cache that fails the next Release link ("Symbols were found but are not matching the assembly"). Delete the heads' `obj/Release` and `bin/Release`.
- Trap: the first `devicectl` launch after an install is sometimes refused. Run it again.
- Trap: iOS Debug builds interpret the app's own code (no JIT on iOS). `System.Numerics` methods such as `Vector2.Transform`, `Lerp` and `Matrix3x2 *` are never precompiled, so per-vertex code using them crawls. Use `Sakura.Framework.Maths.Vector2` and `Matrix3x2` in per-vertex code.

## Handle phones — touch, `Haptics`, `ReturnedToForeground`, `Key.SystemBack`, `TextInputType`, shared files

Keywords: mobile, touch, finger, tap, multi-touch, pinch, OnTouchDown, OnTouchMove, OnTouchUp, TouchEvent, mouse emulation, FromTouch, haptics, vibration, vibrate, IPlatformHaptics, HapticFeedback, PlayTap, background, foreground, resume, suspend, LowMemory, back button, SystemBack, quit, soft keyboard, number pad, TextInputType, audio interrupted, phone call, OutputInterrupted, share sheet, open with, import file, Files app, DataDirectory, SharesFiles

Touch:

- Without any touch handler, the first finger acts as the mouse: a click fires on lift, a move past 10px scrolls the `ScrollableContainer` under it, a 500 ms hold is a right click, and nothing stays hovered after lift. Ordinary UI works on a phone with no extra code.
- For gameplay or gestures, override `OnTouchDown`, `OnTouchMove` and `OnTouchUp(TouchEvent e)` (`ScreenSpacePosition`, `Delta`, `Timestamp`, `Cancelled`). Return `true` from `OnTouchDown` to own that finger.
- `MouseState.FromTouch` tells emulated mouse input apart. `PanZoomContainer` pinches with two fingers.
- In a Debug build, a four-finger tap opens the debug tools.

Haptics:

```csharp
// App.Load: cache it so drawables can resolve it.
Cache(Host.Haptics);

[Resolved(canBeNull: true)]
private IPlatformHaptics? haptics { get; set; }

haptics?.Prepare();                              // before gameplay, so the first tap isn't late
haptics?.PlayTap(0.75f, 0.85f);                  // intensity, sharpness
haptics?.Play(HapticFeedback.Success);           // Selection, Light, Medium, Heavy, Success, Warning, Error
```

- `Host.Haptics.IsSupported` is false on desktops, simulators and emulators. Offer a haptics setting only when it's true.
- Every call is safe from any thread.

Lifecycle:

- Going to the background, the framework pauses its threads, releases the surface, flushes every `ConfigManager` and the log. There's no event for it, and nothing for the app to do.
- `host.ReturnedToForeground` is raised on the update thread when the app comes back, on mobile only. The update thread is stopped in the background, so polling `IWindow.IsActive` can't see the return. Use it to look for files dropped into the app's folder while it was away, and unsubscribe in `Dispose`.
- `host.LowMemory` for dropping caches.
- `IAudioManager.OutputInterrupted` (`Reactive<bool>`) is true while a call, alarm or Siri holds the audio. Pause gameplay on it. The next press takes the audio back.
- Trap: `OnExitRequested` can't veto anything on a phone (`CanBlockExit` is false), and `Exit()` does nothing on iOS. Hide Quit buttons with `!RuntimeInfo.IsMobile`, and save anything important before the app can be backgrounded.
- Android's Back button arrives as `Key.SystemBack`. Left unhandled, it sends the app to the background. Map it to Escape so it goes back a screen, and return `false` at the first screen with nothing open so the OS backgrounds the app as usual.

Keyboards:

- `TextBox.InputType = TextInputType.Number` (also `Text`, `Name`, `Email`, `Username`, `Password`, `NumberPassword`) picks the soft keyboard. It takes effect on the next focus. iPhone number pads get a Done bar that acts as Return. Android pans the window so the field stays above the keyboard.

Files:

- `Storage` is the app data folder: iOS `Library/Application Support`, or `Documents` when `Info.plist` sets `UIFileSharingEnabled` (existing files move over). Android: the private files folder, or `Android/data/<package>/files`, reachable over USB, when the activity overrides `SharesFiles => true`.
- iOS: a file opened with the app from another app arrives as `OnDragDropFile`, the same as a drop on the desktop window, so one override covers both. Files opened in place are copied to a temporary folder first. `ShowOpenFileDialog` shows the document picker and also copies the pick before the callback.
- iOS: `OpenFileExternally` and `PresentFileExternally` open the share sheet (files only, not folders). On Android both return `false`: no sharing yet.
- Trap: Android doesn't deliver "open with" or share intents to the app. The activity handles them: `[IntentFilter]`s, `OnCreate` (only when `savedInstanceState.IsNull()`) and `OnNewIntent`, reading `Intent.Data` or `ClipData` as `content://` URIs through `ContentResolver`, queued until `CreateApp` has run, then handed to the app through a method that's safe from any thread.
- Trap: Android `IntentFilter` path patterns don't backtrack, so a file name with N dots needs N patterns.

## Ship a release — desktop, phones

Keywords: release, publish, ship, installer, Velopack, version, CalVer, AppImage, Linux, APK, AAB, iOS build

- Desktop tools: `references/sakura-application.md`, "Ship a desktop release".
- Games, including Linux AppImages and Android: `references/sakura-game.md`, "Ship a game".
- Phone builds and signing: "Add iPhone, iPad and Android" above.
- An app released whenever something is ready versions with CalVer, `YYYY.MDD.PATCH` (12 October 2026 is `2026.1012.0`), the same scheme as Sakura. See `references/project-setup.md`, "Pick a version scheme".

## Not covered yet

Keywords: gamepad, controller, joystick, Markdown, rich text, Android share sheet

Not covered here: gameplay on a gamepad, Markdown or rich text (Sakura has no Markdown drawable, so a help screen needs its own renderer), and sharing files out of an Android app. For these, read the framework source and the visual tests.
