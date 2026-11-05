# Sakura Game

How I build 2D games on [Sakura Framework](https://github.com/HelloYeew/sakura) for macOS, Windows, Linux, iPhone, iPad and Android: platformers, puzzle games, shooters, card games, visual novels, rhythm games. Written against Sakura `2026.1101.0`.

A public example: https://github.com/HelloYeew/yuuki (a rhythm game)

- Read `references/sakura-framework.md` first. This file only covers what a game does differently or in addition: the solution layout, the gameplay clock, game state, saving, replays, scaling to phones, effects and testing.
- Code style: `references/csharp-style.md`. Project setup and general .NET rules: `references/project-setup.md`.
- Names in `code` are Sakura's API, or C# you'd write the same way in any game. The game's own parts (the world, the import component, the layout service…) are described by what they do: build them and name them however fits the game.
- Most sections apply to any game. "Keep music and gameplay in time" is for games driven by music, and "Build a rhythm game" adds what a rhythm game needs on top; skip both otherwise.

## Contents

- How to search this file
- Rules that apply everywhere
- Lay out a game solution
- Write the Game class
- Scale the UI for every screen
- Make screens declare what they need
- Show overlays, dialogs and notifications
- Pick colors and fonts
- Run gameplay on its own clock
- Keep game state apart from what's drawn
- Keep music and gameplay in time
- Build a rhythm game
- Record and replay
- Save game data
- Migrate saved data in the background
- Import files from everywhere
- Make it feel good
- Draw effects
- Build a level editor
- Test a game
- Ship a game
- Move a feature into the framework
- Keep the code style in a game repo
- Not covered yet

## How to search this file

Keywords: search, grep, find, index, contents, trap

Every `##` section is one ability. Its heading names the main APIs, and its first line starts with `Keywords:` followed by API names and plain words you might search for. Bullets starting with `Trap:` are known mistakes.

```shell
grep -n "^## " references/sakura-game.md                 # list every ability
grep -n "Keywords:.*pause" references/sakura-game.md     # find the section for a word
grep -n "GameplayClock" references/sakura-game.md        # every mention of an API
grep -n "Trap:" references/sakura-game.md                # every known mistake
```

## Rules that apply everywhere

Keywords: rules, must, always, never, checklist, code review

These come on top of the rules in `references/sakura-framework.md`.

1. Gameplay reads time from its own gameplay clock, never from the frame clock, a stopwatch or `DateTime`. Pausing, slow motion, seeking and replays then work for free.
2. Input whose timing matters is handled at the time the OS saw it (`GameplayClock.GetTimeAt(e.Timestamp)`), not at the frame's time.
3. Game state lives in plain objects and arrays, not in drawables. Drawables show the state; they don't own it.
4. Every input source (keys, touch, gamepad, replay) calls the same gameplay entry points.
5. Scores, saves, replays and other game data go in a real database such as SQLite, never in hand-written JSON files.
6. Version every file format the game writes, from the first release.
7. Nothing that must be seen is drawn only by a custom shader. Shaders draw nothing headless or where they fail to build.
8. Layout follows the window's size, not the platform. Check `RuntimeInfo.IsMobile` only for platform behavior, such as hiding Quit.
9. If you also work on Sakura, a feature that another app could use moves into the framework with its tests, then leaves the game (see "Move a feature into the framework").

## Lay out a game solution — `.Game`, `.Game.Desktop`, `.Game.iOS`, `.Game.Android`, `.Game.Resources`, `.Game.Tests`

Keywords: new game, solution layout, slnx, project names, csproj, resource assembly, assets project, ResourceAssembly, netstandard2.1, Directory.Build.props, head project, Program.cs, single instance, file association, Velopack

```
--- sample
 |- sample.slnx
 |- Directory.Build.props
 |- sample.licenseheader
 |- UseLocalFramework.sh
 |- release.sh
 |- Sample.Game                  # all game code: SampleGame : App, screens, gameplay, saving. The only project that references Sakura.Framework
 |- Sample.Game.Resources        # every asset: Fonts/, Samples/, Shaders/, Textures/, Tracks/, Videos/
 |- Sample.Game.Desktop          # Program.cs and icons
 |- Sample.Game.iOS              # Program.cs, Info.plist, icons
 |- Sample.Game.Android          # MainActivity, AndroidManifest.xml, icons
 |- Sample.Game.Tests            # visual and unit tests, runnable
 |- docs/                        # optional: design notes and plans
```

- Names: `<ProjectName>.Game`, then `.Game.<Head>` for each platform, `.Game.Resources` and `.Game.Tests`. A desktop tool uses `.Application` and `.Desktop` instead (see `references/sakura-application.md`). Namespaces follow folders: `Sample.Game.Graphics.Screens`.
- Ship only the heads you need; a desktop-only game has no `.iOS` or `.Android`. The phone heads are in "Add iPhone, iPad and Android" in `references/sakura-framework.md`.
- The data folder name is the same on every head (`"sample"`), so a player moving between devices finds the same layout.
- Pin one Sakura version for `Sakura.Framework`, `Sakura.Framework.iOS` and `Sakura.Framework.Android`.

Assets live in their own `netstandard2.1` project with no code but a handle to its assembly:

```xml
<ItemGroup>
  <EmbeddedResource Include="Fonts\**\*" />
  <EmbeddedResource Include="Samples\**\*" />
  <EmbeddedResource Include="Shaders\**\*" />
  <EmbeddedResource Include="Textures\**\*" />
  <EmbeddedResource Include="Tracks\**\*" />
  <EmbeddedResource Include="Videos\**\*" />
</ItemGroup>
```

```csharp
public static class SampleResourceAssembly
{
    public static Assembly ResourceAssembly => typeof(SampleResourceAssembly).Assembly;
}

// In SampleGame
protected override Assembly ResourceAssembly => SampleResourceAssembly.ResourceAssembly;
protected override string ResourceRootNamespace => "Sample.Game.Resources";
```

- What it buys: a game's assets are large (tens of megabytes of fonts, music, sounds and textures), and in their own assembly they aren't re-embedded on every code change. The game, the tests (`AdditionalResourceAssemblies`) and the shader store all share one handle. The phone heads' own `Resources/` folders can't be mistaken for game assets.
- `Directory.Build.props` keeps the `Resources\**` embed with `Exclude="Resources\**\.DS_Store"` for any other project that has a `Resources/` folder, and the phone heads remove it (`<EmbeddedResource Remove="Resources\**" />`).
- If the tests use only the public API, no `InternalsVisibleTo` is needed.

`Program.cs` on the desktop:

```csharp
VelopackApp.Build().Run();

using AppHost host = new DesktopAppHost("sample", new HostOptions { FriendlyAppName = "Sample" });
using SampleGame game = new SampleGame();
host.Run(game);
```

- A game leaves `HostOptions.AppType` at its default, `Game`.
- A game that opens its own file type (levels, mods, saves to share) adds two small pieces of its own here: a single-instance check (a second launch hands the files it was opened with to the running copy and quits), and on Windows, registering the file type in Velopack's install, update and uninstall hooks.

## Write the Game class — `App`, load order, build info, exit flow

Keywords: SampleGame, App, Game class, root, startup, load order, Cache order, fonts, AddFontFamily, FontScript, main container, build info, version, commit, about screen, exit, quit, confirm exit, unsaved changes, SystemBack, Configurations, ConfigManager, SettingSource, InMemoryConfigManager

`SampleGame.Load` builds everything in this order:

1. Fonts, then `Cache(this)`.
2. Services with no dependencies: the color provider, the layout service (see "Scale the UI for every screen"), `Cache(Host.Haptics)`.
3. The config manager, loaded.
4. The game's data (saves, the content library, scores), only if `Host.Storage` exists, so a headless test can build the game. Read anything that grows with the player's content in the background and merge it in on the update thread, so startup doesn't slow down over time.
5. The app-wide `KeyBindingContainer`.
6. One `DrawSizePreservingFillContainer` holding everything else, in drawing order, for example: background, screen stack, notifications, overlays, dialogs, and the cursor last.
7. Touch effects, outside the scaled container, so they work in the window's own units.

Register fonts in one method that the game and the test app both call, so the two never differ:

```csharp
public static void RegisterFonts(IFontStore fonts, Storage fontStorage)
{
    fonts.AddFontFamily(fontStorage, "DisplayFont");
    fonts.AddFontFamily(fontStorage, "TextFont", hasItalics: true);

    // One fallback per script the game supports.
    fonts.AddFontFamily(fontStorage, "JapaneseFallback", script: FontScript.Japanese);

    // A family without a SemiBold: alias its nearest weight, so text asking for SemiBold still resolves.
    fonts.AddFont(fontStorage, "JapaneseFallback-Medium.ttf", alias: "JapaneseFallback-SemiBold");
}
```

- Background services that need `Update`, a `Scheduler` or DI are `Component`s added to the tree (music, imports, migrations, autosave). The rest are plain objects passed to `Cache`.
- Build info: read the game and framework versions from `AssemblyInformationalVersion` (a default `1.0.0` means a local build: show "development"), the commit from the part after `+`, Debug or Release from `#if DEBUG`, and the build time from the assembly file. Show it in an About screen, and put it in bug reports.
- Settings: `[SettingSource("game.ini")]` on an enum with a doc comment on every value, a `ConfigManager<TEnum>` subclass that registers the defaults (with `MIN_`/`MAX_` constants for ranges), and `InMemoryConfigManager<TEnum>` in tests.
- Exit: `OnExitRequested` returns `true` and starts the game's own exit. Let the current screen veto it (unsaved progress, an editor with changes), then for example go back to the main menu, show a confirm dialog listing any running background work, fade out and call `Host.Exit()`. On phones none of this runs, so save progress before the app can be backgrounded (see "Handle phones" in `references/sakura-framework.md`).
- Android's Back (`Key.SystemBack`) becomes Escape. At the main menu with nothing open, return `false` so the OS sends the app to the background.
- `setting.ValueChanged +=` is only safe in the game class itself, because the game and its settings live equally long. Drawables use `BindValueChanged`.
- Trap: `Cache` a service before adding anything that looks it up while loading. A service cached after the overlay that uses it isn't found by that overlay.

## Scale the UI for every screen — `DrawSizePreservingFillContainer`, UI scale, layout service, phone layout

Keywords: UI scale, zoom, resolution, 1080p, design size, TargetDrawSize, phone layout, small window, tablet, touch layout, hysteresis, game canvas, safe area, Bleed, notch

- The whole game sits in one `DrawSizePreservingFillContainer` with `TargetDrawSize = 1920×1080 / zoom`. The zoom is a UI-scale setting (for example 0.5 to 1.75), multiplied up in a phone-sized window, with a cap.
- A zoom change eases exponentially over a few frames, so dragging the slider glides. A layout change applies at once: easing between two layouts would show neither.
- A small cached layout service tells drawables which layout to use:
  - the current layout: desktop or phone
  - whether the window is phone-sized, with hysteresis: on below one height and off only above a higher one (for example 560 and 640 points), so resizing around the edge doesn't flicker
  - whether the last pointer was a finger. Show touch controls and hints from this, not from the platform.
- A tablet gets the desktop layout, and a small desktop window gets the phone layout. Only quitting checks the platform.
- The play area opts out of the UI scale and draws in its own `DrawSizePreservingFillContainer` sized to a fixed canvas (for example 1920×1080, smaller in a phone window), so the UI-scale setting never changes the game world.
- Safe area: content is already safe. Backgrounds, dims and bar fills set `Bleed = Edges.All` (or `Edges.Horizontal | Edges.Bottom` for a footer) to reach under the notch and the home indicator. Check every screen with Ctrl+F6.

## Make screens declare what they need — a screen base class, `ScreenStack`, screen actions

Keywords: screen, screen stack, shared footer, background, dim, blur, music, cursor hidden, per-screen state, back to menu, screen action, key hints, footer hints, ClearTransforms, UnbindAll

Give the game's screens one base class whose virtual properties say what each screen needs. A subclass of `ScreenStack` applies them to the shared parts (footer, background, music, cursor):

```csharp
public partial class SampleScreen : Screen
{
    public virtual bool ShowsFooter => true;
    public virtual bool DimsBackground => false;
    public virtual bool AllowsMusicChange => true;
    public virtual bool HidesCursor => false;
    public virtual bool ScalesWithUI => true;

    /// <summary>
    /// Return true to stop the game from quitting; call <paramref name="resume"/> to let it carry on.
    /// </summary>
    public virtual bool InterceptExit(Action resume) => false;
}
```

- The stack applies the current screen's state once per change, scheduled with `Scheduler.Add`, after unbinding the previous screen's.
- "Go back to the main menu" exits screens until that one is current, checking again after every exit, because one exit can trigger others.
- A screen can bring its own `KeyBindingContainer` for its actions, and the footer shows their keys (keyboard or gamepad, from the last input used).
- Global keys live in one `KeyBindingContainer` at the root (for example Ctrl+O settings, Alt+arrows volume).
- Trap: Sakura retargets a transform that's still running instead of starting it over. Call `ClearTransforms()` before applying the same transition again (menus and panels that animate on every selection).
- Trap: applying the same screen's state twice stacks a second set of `ValueChanged` handlers. Unbind everything from the previous apply first.

## Show overlays, dialogs and notifications — `FocusedOverlayContainer`, a dialog stack, thread-safe notifications

Keywords: overlay, settings, pause menu, dialog, popup, confirm, notification, toast, progress, background task, cancel, from any thread

- Overlays (settings, a pause menu, dialogs) are `FocusedOverlayContainer`s added and cached by the game. A property on the game that says whether any of them is open helps Back, pausing and the main menu decide what to do.
- Dialogs: one dialog overlay that takes a dialog (icon, title, text, buttons) and shows them one at a time.
- Notifications: the method that posts one is safe from any thread. It puts the notification on a `ConcurrentQueue` that the overlay drains in `Update`.
- Long work (an import, a download, a migration) shows a progress notification with a progress value, a state and a cancel button. The exit dialog lists the ones still running.

## Pick colors and fonts — a palette per screen, font roles, one font helper

Keywords: color, palette, theme, screen colors, contrast, WCAG, accessibility, color blind, font, typeface, font roles, per-script fallback, CJK, Thai, fallback font, FontWeight

- A color provider returns a palette (surface, accent, control, backdrop, text, muted text…) for each part of the game. Each screen caches its own palette, so a shared control takes the colors of the screen it's on.
- Domain colors get their own class (team colors, item rarity, damage types, judgement or rank colors), never literals in drawables.
- Test text contrast against WCAG in a unit test, with a helper that picks a readable text color for a background. Sakura's `ColorExtensions.GetContrastRatio` and `GetRelativeLuminance` arrive after `2026.1101.0`; until then keep the maths in the helper.
- Don't carry meaning by color alone: pair it with a shape, an icon or text, for color-blind players.
- Font roles come from the design, if there is one. For example: one display font for everything that isn't running text (titles, labels, buttons, every number), one text font for prose, dialogue and anything the player types or picks, and a fallback per script the game supports.
- Get every `FontUsage` through one helper that takes the role, the size and the weight, so a font change is one edit.
- Don't name your own weight enum `FontWeight`: it clashes with Sakura's `FontWeights` in every file that needs both.

## Run gameplay on its own clock — `GameplayClock`, pause, slow motion, seek

Keywords: game clock, gameplay clock, pause, resume, slow motion, time scale, hit stop, fast forward, seek, rewind, GameplayClock, StopwatchClock, ProcessFrame, GetTimeAt, input timestamp, HUD

Run everything that is gameplay on one clock of its own, inside a container, so pausing, slowing down or seeking the game is one call:

```csharp
public partial class GameplayClockContainer : Container
{
    private readonly GameplayClock gameplayClock;

    public GameplayClockContainer(IAdjustableClock? source = null)
    {
        gameplayClock = new GameplayClock(source ?? new StopwatchClock());

        // Descendants inherit this clock.
        Clock = gameplayClock;
    }

    public override void UpdateSubTree()
    {
        // GameplayClock isn't a framed clock: advance it before anything in this subtree reads its time.
        gameplayClock.ProcessFrame();
        base.UpdateSubTree();
    }
}
```

- `Stop()` pauses, `Start()` resumes, `Rate` slows down or speeds up (slow motion, hit stop), `Seek(time)` jumps. Transforms, schedulers and `Update` code inside the container all follow it.
- The source is a `StopwatchClock` for a game without music, or a `TrackClock` for one that follows a song (see "Keep music and gameplay in time").
- The HUD, the pause menu and UI sounds sit outside the container, on real time, so they keep working while gameplay is paused or slowed.
- When the moment an input happened matters (a jump on the last frame of a ledge, a parry window, a note), use the time the OS saw it, not the frame's time:

```csharp
private double inputTime(double timestamp)
{
    if (Clock is GameplayClock gameplayClock && !double.IsNaN(timestamp))
        return gameplayClock.GetTimeAt(timestamp);

    return Clock.CurrentTime;
}
```

- Pause automatically when an overlay opens, when the window loses focus, and on a phone when the audio is interrupted (`IAudioManager.OutputInterrupted`).

## Keep game state apart from what's drawn — state, entry points, pools

Keywords: game loop, game state, simulation, model, view, entities, objects, world, board, level, playfield, score, health, lives, fail, game over, win, state machine, pause, input, entry points, DrawablePool, seek

Split gameplay into parts with one job each:

| Part | Job | For example |
| --- | --- | --- |
| The level data | what the level is, loaded from a file: data only | a map, a puzzle board, a wave list, a chart |
| The game state | what's happening now, in plain objects and arrays | positions, health, the board, which notes were hit |
| The world | draws the state and turns input into changes to it; raises events (hit, collected, died, cleared) | the playfield, the arena, the board view |
| The gameplay screen | the state machine (playing, paused, failed, cleared): owns pausing, failing, saving and showing results | |

- Every input source calls the same entry points on the world (for a platformer: move, jump, attack; for a puzzle game: select, swap; for a rhythm game: press, release). Keys, touch, gamepad and replays differ only in who calls them.
- Keep state out of drawables (rule 3). The world draws what's in view each frame and takes drawables from a `DrawablePool<T>`, instead of holding one drawable per object forever. That's what makes seeking, replays, headless tests and large levels work.
- Scoring, health and similar rules are separate classes that subscribe to the world's events. If the game has modifiers, a filter lets one change an event before it's scored or shown.
- Fail or game over: raised once, with the time it happened. What follows is a design choice; for example the action winds down, and the run is saved so its results can be opened.
- Trap: key presses reach the deepest drawables first, so a gameplay key bound to Space can swallow a menu or skip button's Space. Add the overlay's handler before the gameplay container.
- Runs on practice or generated levels aren't saved as scores.

## Keep music and gameplay in time — `TrackClock`, offsets, latency, beat sync

Keywords: rhythm game, music sync, audio sync, beat, BPM, tempo, rate, pitch, lead-in, offset, audio offset, visual offset, latency, display latency, output latency, calibration, TrackClock, DecouplingFramedClock, InterpolatingFramedClock, beat sync, timing points, music-driven

For a game whose gameplay follows music: a rhythm game, or enemies and effects that move to the beat.

One component owns the music. Starting gameplay pauses the menu music and gives gameplay a fresh, non-looping channel for the song, or nothing if the song is missing or can't be decoded (gameplay then runs on a silent `StopwatchClock`). Ending gameplay resumes the menu music where it stopped.

- The clock chain is Sakura's: `TrackClock(channel)` → `GameplayClock`, which decouples from the track, interpolates between audio updates and adds an offset. Use it as the source of the gameplay clock container (see "Run gameplay on its own clock").
- Sakura already removes the output latency it can measure (`TrackClock` implements `IHasOutputLatency`). The player's offsets only cover what it can't.
- Offsets that move both the gameplay and the judging go on `GameplayClock.Offset`: the audio offset (per output device) and a per-song offset.
- Offsets that should only move the drawing go on a separate clock used only by the container of what's drawn in time with the music: the visual offset plus `host.Renderer.DisplayLatencyMilliseconds`.
- Keep offsets per audio device and per display, swap them when the device changes, and save a moment after a change. Record offset changes made during a run in the replay.
- Rate: `TrackClock` changes tempo and keeps pitch by default. Set `PreservePitch = false` for effects such as a record slowing to a stop on a fail.
- Timing points: keep the song's BPM changes in a sorted list with lookups for the timing point at a time, the start of the beat, and whether the song has timing at all, with a default BPM when it doesn't. A lead-in is a count-in of beats, clamped to a sensible range. An intro skip lands on a beat. Visuals follow the beat through a container that raises an event on each beat.
- Trap: a channel reused after a slowed-down moment keeps its slowed `Frequency`, because a new `TrackClock` only sets tempo. Reset `channel.Frequency.Value = 1` on a channel you didn't create.
- Trap: a resume countdown that ignores output latency lands off the beat. Add `channel.OutputLatency`.
- Trap: a background video must follow the music's clock (an `InterpolatingFramedClock` over the playing channel, with `VideoSprite.SyncToClock`), and the outgoing video must stop following it.
- Trap: a channel's `OnEnd` can run off the update thread. Hop back with `Scheduler.Add`.
- Trap: keep a song's exact beat length (444.44 ms, not 444.4), or beat-synced effects drift over a song.
- Trap: looping menu music must handle the end of a track and leave the current song out of a random pick, or the menu goes silent.

## Build a rhythm game — notes, judgement, hit windows, lanes, charts

Keywords: rhythm game, music game, notes, hit objects, judgement, hit window, timing window, accuracy, combo, perfect, great, miss, lanes, hold notes, slides, flicks, chart, beatmap, difficulty, intro skip, lead-in, scroll speed, note skin

On top of the general sections, which all apply: gameplay runs on a `TrackClock`-driven gameplay clock ("Keep music and gameplay in time"), and the parts are the chart (level data), the judgement state, the playfield (the world) and the gameplay screen ("Keep game state apart from what's drawn").

- Judge each press at the time the OS saw it (`GameplayClock.GetTimeAt(e.Timestamp)`, see "Run gameplay on its own clock"). Judging at the frame's time adds up to a whole frame of error, which players feel.
- Hit windows: a fixed set of tiers in milliseconds either side of the note (tightest first, then wider ones, then a miss window), with releases of hold notes judged more leniently (for example 1.5 times wider).
- Judgement state is arrays indexed by note (`judged[]`, `results[]`), not fields on note drawables. Each frame the playfield binary-searches the notes' start times for what's in view and takes note drawables from a `DrawablePool<T>`. Seeking, replays, headless re-judging and charts with thousands of notes then all work.
- Input entry points for a lane game: press lane, release lane, plus touch down, slide and flick for phones. Keys, touch and replays all call these.
- Scoring: a score processor normalized to a fixed maximum (for example accuracy plus combo), health that fails the run once it runs out, and ranks computed from accuracy on load rather than stored.
- The lead-in is a count-in of beats before the first note, clamped to a sensible range. An intro skip lands on a beat.
- Haptics: one tap per judgement, firmer for better judgements, nothing for a miss, nothing during replays.
- Charts: the library reads each chart's metadata only; the notes are loaded for a play and unloaded after. Version the chart format, keep a hash of the chart in each replay, and keep the rules version on each score, so old scores can be re-judged when the rules change ("Migrate saved data in the background").
- The editor has a timing tab (BPM changes, offset), draws the song with `Waveform`, and test-plays through the same gameplay screen.
- Trap: key presses reach the deepest drawables first, so a lane bound to Space can swallow the intro skip. Add the skip handler before the gameplay container.
- Trap: keep a chart's exact beat length (444.44 ms, not 444.4), or notes and beat-synced effects drift over a song.
- Plays on generated or practice charts aren't saved as scores.

## Record and replay — inputs with times, re-simulation, seeking

Keywords: replay, record, ghost, playback, watch again, kill cam, seek replay, rewind, deterministic, re-simulate, headless

- A replay stores the inputs with their times (and settings that change the outcome, such as offsets), never frames or positions. It carries a format version and a hash of the level, and refuses to play on a level that changed since.
- Playback feeds the inputs to the same world entry points as live play. The game state must come out the same for the same inputs: no unseeded randomness, no frame-rate-dependent maths in game state (draw-only effects can do as they like).
- Seeking re-runs the play from the start at a fixed number of updates per second (for example 60) with sounds muted, then settles the drawables at the target time. With state out of drawables (rule 3) this is fast.
- A headless run plays a replay through a world that's never added to the tree, on a pool thread: to re-score old runs, or to check a submitted score.
- Write a regression test for every replay fix.

## Save game data — a real database, format versions, damaged data

Keywords: save, save game, progress, persist, persistence, database, SQLite, scores, replays, settings, library, file format, version, migration, corrupt data, damaged data, lazy load, metadata

- Store saves, scores, replays and the content index in a real database, such as SQLite, not in hand-written JSON files. There's no proven database setup in a Sakura game yet (see "Not covered yet"). Whatever you pick has to run on iOS, where there's no JIT, and on Android: try it on both heads early.
- Damaged data is logged with `Logger.Error(message, ex)`, and the game carries on without it. It never crashes the game, and it never disappears silently: a file you can't read is always logged.
- Load what lists need and nothing else: a level select reads each level's metadata only and remembers where the rest is. The full level is loaded to play it and unloaded after.
- Version each thing separately: the level format (a current version number, with a mapping from older versions), the save and replay formats, and the scoring rules (a rules version on each score).
- Recompute derived values on load instead of trusting what was saved (ranks, totals, unlocks).
- When an import has an ID that already exists, replace the older copy. A reader that keeps the first match it finds shows the stale one.

## Migrate saved data in the background — re-scoring old runs

Keywords: migration, upgrade, rules change, rescore, background job, backup, one step per frame

When the scoring rules change, re-score older runs from their replays, in the background:

- A `Container` added to the game queues the runs under older rules.
- Each `Update` it starts at most one step: read the replay and level in `Task.Run`, re-run them headless on a pool thread, and come back with the result.
- It applies the new totals only if the outcome matches the original (a clear is still a clear, a fail still fails), and marks runs it tried so failures aren't retried.
- It backs up the scores once before the first change, saves in batches, pauses during gameplay, and shows a cancellable progress notification.

## Import files from everywhere — drag and drop, opened files, a second launch, phones, the data folder

Keywords: import, user content, levels, mods, custom content, drag and drop, drop file, open with, file association, package file, zip, second instance, single instance, share, Files app, Finder, content URI, Android, iOS, ReturnedToForeground, watch folder, first run, bundled content

For a game that takes files from the player (custom levels, mods, shared saves). Every way in goes to one import `Component`:

| Source | How it reaches the import |
| --- | --- |
| A file dropped on the window | `OnDragDropFile` on the game |
| A file opened with the game on iOS | also `OnDragDropFile`: Sakura delivers it that way |
| A file opened while the game is running (desktop) | the second launch hands it over through the single-instance check |
| A file opened with the game on Android | the activity passes it on, reading `content://` links through `ContentResolver` |
| Files dropped into the data folder (Finder's Files tab, Android over USB) | checked at launch, when the window is activated, and on `host.ReturnedToForeground` |
| An Import button in the settings | `ShowOpenFileDialog` |
| The content that ships with the game | copied once on first run, remembered in a setting |

- Wait a moment (for example 250 ms) for more files before importing, because a multi-file drop arrives one file at a time.
- The import's entry point is safe from any thread: it hops to the update thread with `Scheduler.Add`.
- Don't check the data folder during gameplay or a replay.
- Scan into a fresh collection in the background, then merge it into the live one on the update thread.
- Trap: `Scheduler` is created on first use. Read it on the update thread into a local before `Task.Run`, then call the local from the background.

## Make it feel good — haptics, impact sounds, touch effects

Keywords: haptics, vibration, rumble, feedback, game feel, juice, impact, hit stop, screen shake, hit sounds, touch effects, ripple, tap effect

- Haptics: `haptics?.Prepare()` in `LoadComplete` and again after resuming, or the first tap comes late. One `PlayTap(intensity, sharpness)` per event the player should feel, scaled to how much it matters (a perfect hit or a heavy landing firm and crisp, a small one softer, nothing for a miss). Nothing during replays. Offer the setting only when `IsSupported` (see "Handle phones" in `references/sakura-framework.md`).
- Impact sounds play from a component outside the gameplay clock, on real time, so slow motion doesn't drag them.
- Hit stop and screen shake: a short `Rate` dip on the gameplay clock, and a quick move transform on the play area's container.
- Touch effects sit outside the scaled UI, in window units, and hide while the cursor is hidden.

## Draw effects — custom shaders, additive light, `ParticleField`, `RingArc`, pooling

Keywords: effects, VFX, glow, light, bloom, rays, rings, particles, explosion, trail, results screen, shader, custom shader, additive, blur, frosted glass, white texture, tint, pool, DrawablePool, shared texture, sprite sheet

Keep one store for all of the game's shaders, over its resource assembly:

```csharp
public static class GameShaders
{
    public static readonly Storage STORAGE = new EmbeddedResourceStorage(SampleResourceAssembly.ResourceAssembly, "Sample.Game.Resources.Shaders");

    public static readonly CustomShaderStore STORE = new CustomShaderStore(STORAGE);
}

// A drawable:
protected override VertexTopology Topology => VertexTopology.Triangles;

protected override DrawNode CreateDrawNode() => new CustomShaderDrawNode<GlowBlock>(GameShaders.STORE, "glow.frag", nameof(GlowBlock), block);
```

- How to write one: "Write a custom shader" in `references/sakura-framework.md`. Give the game's shaders one shared include (for example `game.glsl`) that includes `sh_Custom.glsl` and adds the game's colors and a helper for additive light. Colors go in linear (`ColorExtensions.SrgbToLinear`).
- A unit test runs `ShaderChecks.Problems` on every shader, so a shader that wouldn't build on one renderer fails `dotnet test`.
- Light and glow: an additive shader (a screen-edge glow, light rays), or a `BufferedContainer` blur (a color drain on a results screen, frosted panels).
- Particles: one `ParticleField` per effect (sparks, dust, petals, debris), cleared and refilled every frame, drawn in one go. Rings, spinners and cooldown indicators: `RingArc`.
- Textures are white and tinted in code, so one texture serves every color.
- Pools: a `DrawablePool<T>` for anything that appears and disappears often (bullets, enemies, pickups, notes, list cards, hit bursts). Large shared art (covers, portraits) is a shared texture released by key, with recently released ones kept for a while.
- Trap: free a texture with `host.Renderer.ScheduleToDrawThread(texture.Dispose)`, and dispose the `Texture`, never its backend. Shared textures go back with `ReleaseSharedTexture(key)`.
- Trap: check draw order in composite effects. A part added before the piece it sits on draws behind it.

## Build a level editor — undo, editor clock, test play

Keywords: editor, level editor, map editor, chart editor, tile editor, placement, timeline, undo, redo, history, waveform, test play, save prompt, new level

- The editor is a game screen with tabs or tools (for example place, select, properties, test) switched by a reactive.
- An editor session holds the level being edited, its history and, for a timed level, the clock. The level in the editor is a mutable copy that builds a fresh level to save.
- Undo keeps whole snapshots, to a capped depth, and knows whether there are unsaved changes. Simple, and plenty for most levels.
- Test play reuses the gameplay screen with the edited level, so what you test is what players get.
- A music-driven level uses the same `GameplayClock` chain in the editor, clamped to the song, and draws the song with Sakura's `Waveform` on a timeline.
- The editor screen vetoes the game's exit and asks to save first.

## Test a game — fake haptics, generated audio, seek instead of wait, the whole game in a scene

Keywords: test, testing, visual test, headless, NUnit, test app, test scene, fake, mock, haptics test, audio test, WAV, seek, whole game test, regression test, TestBrowserApp, AdditionalResourceAssemblies, PressKeyOnce

The test app loads the game's resource assembly alongside its own:

```csharp
public partial class SampleTestApp : TestBrowserApp
{
    public override void Load()
    {
        base.Load();
        // The same font method the game calls.
        SampleGame.RegisterFonts(FontStore, new EmbeddedResourceStorage(SampleResourceAssembly.ResourceAssembly, "Sample.Game.Resources").GetStorageForDirectory("Fonts"));
        Add(new CursorContainer());
    }

    protected override IEnumerable<(Assembly Assembly, string RootNamespace)> AdditionalResourceAssemblies
        => [(SampleResourceAssembly.ResourceAssembly, "Sample.Game.Resources")];
}
```

- Base scenes: one over `TestScene` that caches the color provider; one over `ManualInputManagerTestScene` that also clears content and resets input in `[SetUp]`; one for whole screens that builds a screen stack once per fixture with `DisposeOnRemoval = false` and adds it again in each test.
- Each test caches only what it needs: a fake `IPlatformHaptics` that records taps, a `NativeStorage` on a temporary folder, an `InMemoryConfigManager`, an audio manager with `DisposeOnRemoval = false`.
- Generate test audio instead of shipping it: a small helper that writes a short sine-wave WAV to a temporary folder is enough for anything that needs music or a sound.
- Drive gameplay by seeking the gameplay clock and pressing keys, instead of waiting:

```csharp
Cache<IPlatformHaptics>(fakeHaptics);

AddStep("go to the first jump", () => gameplayClock.Seek(1000));
AddStep("jump", () => PressKeyOnce(Key.Space));
AddAssert("landing was felt", () => fakeHaptics.Taps.Count == 1);
```

- Test the game state without drawing where you can: build the level data and the state, call the entry points, assert on the state. Rule 3 makes this possible.
- The whole game in a scene: `new SampleGame { Clock = Clock }`, `SetHost(host)`, `OnReady()`, `AddApp(game)`. Use a new game in each test: a shown game is disposed when the scene clears.
- Logic with no drawables (level loading, scoring, layout maths, colors, shaders) gets plain NUnit tests.
- Every fix comes with a regression test.
- Run `dotnet test Sample.Game.Tests` before every release, in CI or by hand.
- Trap: headless audio plays in real time while the test clock runs fast. Seek music to its end instead of waiting for it.
- Trap: custom shaders draw nothing headless, and text measures 0 wide. Mark those tests `[VisualTestOnly("…")]`.
- Trap: a sound player that skips missing samples (as one for player-made content must) hides a missing file in tests too. A passing test doesn't prove a sound exists.

## Ship a game — desktop, Linux, Android, iPhone

Keywords: release, publish, ship, version, CalVer, Velopack, vpk, installer, AppImage, Linux, macOS bundle, Info.plist, file type, game mode, Windows, aumid, Android, APK, AAB, keystore, version code, iOS, GITHUB_OUTPUT

One `sh release.sh [patch]` builds everything into `Releases/`:

- Version: for a game released whenever something is ready, `YYYY.MDD.PATCH` (CalVer) by the release date (see `references/project-setup.md`, "Pick a version scheme"). Android's version code is `YYMMDDPP` so it always grows.
- macOS: `vpk bundle`, then `plutil` adds the display name, the game's file type if it has one (`CFBundleDocumentTypes`), the app category (`public.app-category.games` or a more specific one such as `public.app-category.puzzle-games`) and game mode (`LSSupportsGameMode`, `GCSupportsGameMode`), and `xcrun actool` builds the icon.
- Windows: x64, x86 and arm64, with `--aumid` (the Windows app ID).
- Linux: `linux-x64` and `linux-arm64` AppImages (`vpk "[linux]" pack`). Building them on a Mac needs `brew install squashfs`.
- Android: APK and AAB, signed when the keystore, key alias and password environment variables are set, otherwise with the debug key and a warning. Never ship a debug-signed build.
- iOS: build with the same version (`-p:Version=… -p:ApplicationDisplayVersion=… -p:ApplicationVersion=…`) and install with a small script around `xcrun devicectl`.
- Write the version to `$GITHUB_OUTPUT` for a release workflow.
- Every head's details (signing, devices, traps): "Add iPhone, iPad and Android" in `references/sakura-framework.md`, and "Ship a desktop release" in `references/sakura-application.md`.

## Move a feature into the framework — build in the game, then upstream

Keywords: upstream, framework change, move to framework, reusable, contribute, Sakura PR, bump framework

If you also work on Sakura: build a feature in the game, move it into Sakura with tests and a benchmark, release Sakura, bump the game to that version, then delete the game's copy and its tests.

## Keep the code style in a game repo — `.editorconfig`, `.sln.DotSettings`, license header

Keywords: code style, editorconfig, DotSettings, Rider, license header, MIT, formatting

- Follow `references/csharp-style.md`. Copy `.editorconfig` and `<project-name>.sln.DotSettings` from `references/project-setup.md`, not from another project's copy, which may be out of date.
- License header: the one matching the game's `LICENSE` file, set in `file_header_template` and `<project-name>.licenseheader` (see `references/project-setup.md`, "Copy the shared build files").
- Every `Drawable` subclass is `partial`, including small helper containers, and test scenes expose shared parts as PascalCase properties, not public fields.
- Doc comments explain why, in plain English, on almost every member, as `references/csharp-style.md` asks.

## Not covered yet

Keywords: gamepad, controller, physics, collision, tilemap, pathfinding, networking, online, multiplayer, leaderboard, accounts, cloud save, achievements, CI, database, SQLite, iOS release

Not covered here: gameplay on a gamepad, physics and collision, tile maps, pathfinding, networking and online play, leaderboards, accounts and cloud saves, achievements, a database setup (such as SQLite) for saves, scores and replays, CI, and an iOS build inside `release.sh`.
