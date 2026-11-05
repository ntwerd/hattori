# Sakura Application

How I build desktop tools ("applications") on [Sakura Framework](https://github.com/HelloYeew/sakura): apps with a custom-drawn UI and settings (and, where a tool needs it, image work on the GPU), rather than games. Written against Sakura `2026.1101.0`.

- Read `references/sakura-framework.md` first. This file only covers what a desktop tool does differently or in addition.
- Games: `references/sakura-game.md`. Code style: `references/csharp-style.md`. General .NET setup: `references/project-setup.md`.

## Contents

- How to search this file
- Rules that apply everywhere
- Lay out an application solution
- Scale the UI like a desktop app
- Theme the whole app from one provider
- Confirm before closing with unsaved work
- Run one job at a time and show it
- Decode images
- Process images without running out of memory
- Pan and zoom an image
- Show which build is running
- Test controls and screens
- Ship a desktop release

## How to search this file

Keywords: search, grep, find, index, contents, trap

Every `##` section is one ability. Its heading names the main APIs, and its first line starts with `Keywords:` followed by API names and plain words you might search for. Bullets starting with `Trap:` are known mistakes.

```shell
grep -n "^## " references/sakura-application.md                 # list every ability
grep -n "Keywords:.*zoom" references/sakura-application.md       # find the section for a word
grep -n "MagickImage" references/sakura-application.md           # every mention of an API
grep -n "Trap:" references/sakura-application.md                 # every known mistake
```

## Rules that apply everywhere

Keywords: rules, must, always, never, checklist, code review

These come on top of the rules in `references/sakura-framework.md`.

1. The host runs with `AppType = AppType.Application`.
2. The UI is laid out at 1:1 logical points, never stretched to a fixed design size.
3. Every color comes from one cached color provider. No literal colors in controls.
4. Heavy work runs off the update thread, one job at a time, with something on screen saying it's running.
5. The app shows which build is running: version, commit, Debug or Release.

## Lay out an application solution — `.Application`, `.Desktop`, `.Application.Tests`, `.CommandLine`

Keywords: new project, solution layout, slnx, csproj, project names, Application, Desktop, CommandLine, InternalsVisibleTo, AppType, VSync, Program.cs, HeadlessAudioManager

```
--- sample
 |- sample.slnx                   # dotnet sln migrate, then delete the .sln
 |- Directory.Build.props
 |- UseLocalFramework.sh
 |- release.sh
 |- Sample.Application            # renamed from the template's Sample: App subclass, screens, controls, Resources/
 |- Sample.Desktop                # Program.cs, icon and packaging only
 |- Sample.Application.Tests      # visual and unit tests, runnable
 |- Sample.CommandLine            # optional: dev tools, batch jobs
```

- Only `.Application` references `Sakura.Framework`. The other projects get it through their reference to `.Application`.
- `.Application.csproj` opens internals to the projects that use it:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Sample.Desktop" />
  <InternalsVisibleTo Include="Sample.CommandLine" />
  <InternalsVisibleTo Include="Sample.Application.Tests" />
</ItemGroup>
```

- `Program.cs` sets the host up as an application:

```csharp
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        using AppHost host = new DesktopAppHost("sample", new HostOptions { AppType = AppType.Application });
        using App app = new SampleApp();
        host.Run(app);
    }
}
```

- `AppType.Application` uses VSync, never busy-spins the update and draw threads, lets the screen sleep, uses the default GC mode and doesn't take audio focus. The default, `Game`, is wrong for a tool: it spins two threads and keeps the display awake.
- Trap: settings are written with their defaults, so a machine that ran the app before you set `AppType` keeps `FrameLimiter = Limit2x` in its `framework.ini`.
- An app that makes no sound overrides `CreateAudioManager()` to return `new HeadlessAudioManager()`, so it never opens an audio device.
- On a phone, a tool also drops the manifest's game category (see `references/sakura-framework.md`, "Add iPhone, iPad and Android").

## Scale the UI like a desktop app — 1:1 points with a minimum size

Keywords: scaling, scale UI, DPI, window size, resize, minimum size, small window, laptop screen, DrawSizePreservingFillContainer, logical points, text size, layout size

`DrawSizePreservingFillContainer` stretches the whole UI to a fixed design size (1920×1080). That suits a game, but in a tool it makes text bigger or smaller than the OS asks for, and a bigger window shows the same content larger instead of showing more of it.

Lay out at 1:1 logical points instead, and scale down only when the window is smaller than the layout's minimum (1280 points wide works for a tool with side panels). Extra space goes to the content.

```csharp
/// <summary>
/// Lays its content out at 1:1 logical points, and scales it down only when the window is narrower than the layout's minimum.
/// </summary>
public partial class DesktopScalingContainer : Container
{
    private const float min_width = 1280;

    private readonly Container content;

    protected override Container Content => content;

    public DesktopScalingContainer()
    {
        RelativeSizeAxes = Axes.Both;
        AddInternal(content = new Container());
    }

    public override void Update()
    {
        base.Update();

        float scale = Math.Min(1, DrawSize.X / min_width);
        content.Scale = new Vector2(scale);
        content.Size = DrawSize / scale;
    }
}
```

- This is a sketch: add a minimum height the same way if the layout needs one.
- Put it at the root in `App.Load`, around the screen stack and overlays.
- Trap: the headless test window is 800×600, below the minimum, so screen tests run scaled down. Don't assert on absolute sizes from a screen test.

## Theme the whole app from one provider — color provider, live theme switch

Keywords: theme, dark mode, light mode, colors, palette, color provider, design tokens, Surface, Accent, Danger, switch theme, live theme change, contrast, WCAG

```csharp
public interface ISampleColorProvider
{
    Color Background { get; }
    Color Surface { get; }
    Color Accent { get; }
    Color Danger { get; }
    Color Text { get; }
    Color TextMuted { get; }
}

// App.Load
Cache<ISampleColorProvider>(colors);
```

- Controls ask the provider for a role (`Surface`, `Accent`, `Danger`), never for a literal color. Changing the look then means changing one class.
- Name font roles the same way (`SampleFont.Title`, `Heading`, `Body`, `Caption`, `Value`).
- Live theme switch: when the theme setting changes, swap the provider's palette, then walk the tree once from the root (`InternalChildren`, recursively) and tell every themeable control to re-read its colors, for example through your own small `IThemeable.ApplyTheme()` interface. Controls don't subscribe to the theme setting themselves.
- Why walk instead of subscribe: hundreds of controls each binding the same setting means hundreds of listeners on a reactive that lives as long as the app, and `OwnReactive` on a shared reactive would cut everyone's binding (see `references/sakura-framework.md`, "React to value changes").
- Apply the theme with duration `0` during load, so nothing animates on first show.
- Check text contrast against WCAG in a unit test, in both themes. Sakura's `GetContrastRatio` is newer than `2026.1101.0`; until the next release, keep a small helper of your own.

## Confirm before closing with unsaved work — `OnExitRequested`, `Host.Exit()`

Keywords: quit, close window, exit, unsaved changes, save prompt, confirm exit, dialog, discard changes

```csharp
protected override bool OnExitRequested()
{
    if (!workspace.HasUnsavedChanges)
        return false;

    confirmDialog.Show("Discard unsaved changes?", onConfirm: () => Host.Exit());
    return true;
}
```

- Return `true` to keep the app open, show a `FocusedOverlayContainer` dialog, and call `Host.Exit()` once the user confirms. `confirmDialog.Show(…)` here is your own dialog's method.
- A quick second close forces the exit, so a stuck dialog can't trap the user.
- A screen that owns the unsaved work (an editor) is the one that knows. Let the App ask the current screen rather than tracking it globally.
- On phones none of this runs: a phone app can't refuse to close (see `references/sakura-framework.md`, "Handle phones").

## Run one job at a time and show it — busy indicator, queued request

Keywords: busy, progress, spinner, loading, working, wait cursor, queue, one at a time, debounce, slider, re-run, stale result, cancel, responsive

Heavy work (an image adjustment, a long computation) follows the pattern in `references/sakura-framework.md`, "Run slow work without freezing the UI", plus two rules: only one job runs at a time, and the user can see it running.

```csharp
private bool isRunning;
private bool runQueued;

private void requestRun()
{
    // A job is running: remember that another was asked for, and run it once with the latest values.
    if (isRunning)
    {
        runQueued = true;
        return;
    }

    isRunning = true;
    busy.Show("Adjusting…");

    double strength = strengthSlider.Current.Value;
    var scheduler = Scheduler;

    Task.Run(() =>
    {
        try
        {
            AdjustResult result = adjust(strength);
            scheduler.Add(() => finish(result));
        }
        catch (Exception ex)
        {
            scheduler.Add(() =>
            {
                Logger.Error("Image adjustment failed", ex);
                finish(null);
            });
        }
    });
}

private void finish(AdjustResult? result)
{
    isRunning = false;
    busy.Hide();

    if (result.IsNotNull())
        preview.Show(result);

    if (runQueued)
    {
        runQueued = false;
        requestRun();
    }
}
```

- Debounce a slider before `requestRun` (`Scheduler.AddDelayed`, 150 ms).
- Starting a new job for every slider move piles up background work and shows results out of order. One job plus one queued request always ends on the latest values.
- The busy indicator is an overlay with a message and `CursorState.Wait`. It shows what's happening; it doesn't block input unless the job can't tolerate changes.

## Decode images — built-in `RoutingImageLoader`, `CreateImageLoader`, a Magick loader for color and RAW

Keywords: decoder, built-in decoder, default loader, do I need ImageMagick, image loader, decode, JPEG, PNG, TIFF, WebP, RAW, NEF, CR2, DNG, ARW, camera file, ICC profile, color space, color management, sRGB, EXIF orientation, rotated image, ImageMagick, Magick.NET, ImageSharp, stb, magenta tint

- Usually you need nothing here. The default `RoutingImageLoader` already:
  - decodes JPEG and PNG with stb, and anything stb can't read (WebP, TIFF, QOI, PBM) with ImageSharp
  - applies EXIF rotation: rotated photos go to ImageSharp, which turns them upright
  - follows `ImageLoadOptions`, so `textures.Get(path, ImageLoadOptions.MaxDimension(512))` decodes small
- What it doesn't do: color management (an embedded ICC profile is ignored, so Adobe RGB or wide-gamut files show the wrong colors) and camera RAW files.
- Override `App.CreateImageLoader()` only for one of those two, with a Magick.NET loader (`IImageLoader`) that:
  - converts to sRGB with `TransformColorSpace(ColorProfiles.SRGB)`
  - calls `AutoOrient()`
  - follows `ImageLoadOptions`: a decode-size hint, a center crop for `Fill`, downscaling only
  - returns RGBA bytes as `ImageRawData`
- RAW camera files need `MagickReadSettings` with `new DngReadDefines { UseAutoWhiteBalance = true, OutputColor = DngOutputColor.SRGB }`.
- Trap: without auto white balance, some RAW files come out magenta.
- Package: `Magick.NET-Q16-AnyCPU` (16 bits per channel).

## Process images without running out of memory — `ImageRawData.Rent`, `ArrayPool`, `Mat.AsSpan`, `NativeMemoryTracker`

Keywords: image processing, filters, OpenCV, OpenCvSharp, Mat, Magick, MagickImage, 16-bit, bit depth, large object heap, LOH, GC, garbage collection, memory leak, out of memory, pooled buffer, native memory, unmanaged memory, statistics, clone, commit, edit

- Keep the image data you compute on at full bit depth in `MagickImage` (Q16) or `Mat`. `ImageRawData` and textures are 8-bit RGBA only, so convert just the copy you display.
- Edit a clone of the original, never the original itself, so edits don't stack and a failed edit leaves the current image intact: keep the original and the current image, and give the editing class a way to clone the original and a way to commit a finished result.
- Full-resolution buffers are big: a 4K float frame is 33 MB. Every new array that size is a large-object-heap block that only a full GC frees, so don't allocate them per call:
  - Pixels for the GPU: `ImageRawData.Rent(width, height)` (pooled). Write through `GetWritableSpan()` and `Dispose()` to return it. `ImageRawData.CopyFrom(width, height, span)` copies existing data.
  - Scratch arrays: `ArrayPool<float>.Shared.Rent(n)`, returned in a `finally`.
  - Trap: a rented array can be longer than asked and holds the previous user's data. Only use `[0, n)` and never assume zeros.
  - Out of a `Mat`: `mat.AsSpan<byte>()[..n].CopyTo(target)`, not `GetArray` or `Marshal.Copy`, which allocate.
- Dispose every `Mat` and `MagickImage` (`using var`).
- Make unmanaged memory visible in the statistics window (Ctrl+F2): `NativeMemoryLease lease = NativeMemoryTracker.Add(NativeMemoryCategory.Other, bytes);`, disposed together with the object. Track each Magick image this way.
- Packages, only when the tool needs them: `OpenCvSharp5` plus a runtime package for each platform you ship, and `Magick.NET-Q16-AnyCPU` for color management or RAW. For simple image work, ImageSharp 3.x is enough (see `references/project-setup.md`, "Use these libraries").

## Pan and zoom an image — `PanZoomContainer`, drag capture, `ToLocalSpace`

Keywords: zoom, zoom in, zoom out, pan, scroll to zoom, drag to move, canvas, viewport, fit to window, image viewer, map pointer to image, pinch

- `PanZoomContainer` gives wheel zoom (`Zoom`, `MinZoom`, `MaxZoom`, `ZoomSpeed`), mouse panning (`PanPosition`) and two-finger pinch with no extra code.
- For more (fit to window, annotations drawn on the image, panels floating over it), build your own canvas: everything on the image lives in one container that is scaled and moved as one, and the image is a sprite that returns `true` from `OnDragStart` and reports `e.Delta` from `OnDrag`.
- Map the pointer into the canvas with `canvas.ToLocalSpace(e.ScreenSpaceMousePosition)`.
- Draw annotations with one `PlotCanvas` inside the canvas, not one drawable per mark.
- Panels over the canvas: override the canvas's `ReceivePositionalInputAt` so points under the panels don't reach it.

## Show which build is running — `BuildInfo`, version, commit, configuration

Keywords: version, about, build info, commit, Debug, Release, traceability, AssemblyInformationalVersion, build time

Show the build in an About screen or a status bar, so a bug report can be traced to the build that made it. If the app exports results, write the build into those files too.

- Version: `AssemblyInformationalVersion` of the app assembly. A default `1.0.0` means a local build; show "development".
- Commit: the part of the informational version after `+`, first 7 characters.
- Sakura version: the same, read from the `Sakura.Framework` assembly.
- Configuration: Debug or Release, from `#if DEBUG` (see `references/project-setup.md`, the `DebugUtils.IsDebugBuild` helper).
- Build time: the assembly file's write time. It's `null` for a single-file publish, so show nothing then.

## Test controls and screens — base scenes for one control and for whole screens

Keywords: test base class, control test, screen test, theme test, light theme, screen stack test, safe area test, headless, settings in tests

Give each kind of test its own base scene that caches what the App normally provides:

- A control scene: caches the color provider, builds one control through a `CreateContent(() => …)` method of its own, and has an `AddLightThemeStep()` that rebuilds it in the light theme, so every control is checked in both themes.
- A screen scene: caches a screen stack, a config manager built with `null` storage (nothing touches the developer's real settings), and any overlay a screen opens. Keep the stack with `DisposeOnRemoval = false` and add it again in each test.
- Wrap screen content in `new SafeAreaDefiningContainer { OverrideSafeArea = new MarginPadding() }`, so the headless window's safe area never leaks in.
- The whole app in one scene: create the App, `SetHost(host)`, then `AddApp(app)`, with a new App per test.
- Trap: NUnit reuses one fixture for every test in the class, and `Clear()` before each test disposes what the loader built. Otherwise tests run against a disposed, detached stack and pass with every size reading zero. Keep long-lived parts out of `Clear`, or rebuild them in `[SetUp]`.
- Trap: the headless window is below the 1280-point minimum layout (see "Scale the UI like a desktop app"), so screens are tested scaled down.

## Ship a desktop release — Velopack, self-contained publish, CalVer

Keywords: release, publish, build installer, package, update, auto-update, Velopack, vpk, self-contained, runtime identifier, RID, win-x64, win-x86, win-arm64, osx-arm64, linux-x64, AppImage, version number, CalVer, Windows ARM64, VC++ redistributable

- Publish self-contained for each platform and pack with Velopack (`vpk`). `VelopackApp.Build().Run();` is the first line of `Main`.
- One `release.sh` (and `release.ps1` for Windows) does it all:
  - `dotnet publish Sample.Desktop -c Release --self-contained -r osx-arm64 -p:Version=$VERSION`, and the same for every other platform you ship (`win-x64`, `win-x86`, `win-arm64`)
  - Linux, if you ship it: `linux-x64` and `linux-arm64` packed as AppImages with `vpk "[linux]" pack` (building them on a Mac needs `brew install squashfs`)
  - for a tool released whenever something is ready, the version is CalVer, `YYYY.MDD.PATCH` (see `references/project-setup.md`, "Pick a version scheme")
- Trap: on Windows on ARM, if `bass` or `libsakura-spirv` fails to load ("Unable to load DLL"), install the VC++ Redistributable for ARM64 (https://aka.ms/vs/17/release/vc_redist.arm64.exe). Running the x64 build under emulation avoids it.
- Phones: `references/sakura-framework.md`, "Add iPhone, iPad and Android". Velopack has no phone support.
